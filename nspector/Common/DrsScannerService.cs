using nspector.Common.Helper;
using nspector.Native.NVAPI2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using nvw = nspector.Native.NVAPI2.NvapiDrsWrapper;

namespace nspector.Common
{
    internal class DrsScannerService : DrsSettingsServiceBase
    {
        public DrsScannerService(DrsSettingsMetaService metaService, DrsDecrypterService decrypterService)
            : base(metaService, decrypterService)
        { }

        internal List<CachedSettings> CachedSettings = new List<CachedSettings>();
        internal List<string> ModifiedProfiles = new List<string>();
        internal HashSet<string> UserProfiles = new HashSet<string>();

        // Most common setting ids as start pattern for the heuristic scan
        private readonly HashSet<uint> _commonSettingIds = new HashSet<uint>
        {
            0x1095DEF8, 0x1033DCD2, 0x1033CEC1, 0x10930F46, 0x00A06946, 0x10ECDB82,
            0x20EBD7B8, 0x0095DEF9, 0x00D55F7D, 0x1033DCD3, 0x1033CEC2, 0x2072F036,
            0x00664339, 0x002C7F45, 0x209746C1, 0x0076E164, 0x20FF7493, 0x204CFF7B
        };

        private bool CheckCommonSetting(IntPtr hSession, IntPtr hProfile, NVDRS_PROFILE profile,
            ref int checkedSettingsCount, uint checkSettingId, bool addToScanResult,
            ref HashSet<uint> alreadyCheckedSettingIds)
        {
            if (checkedSettingsCount >= profile.numOfSettings)
                return false;

            var setting = new NVDRS_SETTING { version = nvw.NVDRS_SETTING_VER };

            if (nvw.DRS_GetSetting(hSession, hProfile, checkSettingId, ref setting) != NvAPI_Status.NVAPI_OK)
                return false;

            if (setting.settingLocation != NVDRS_SETTING_LOCATION.NVDRS_CURRENT_PROFILE_LOCATION)
                return false;

            if (!addToScanResult && setting.isCurrentPredefined == 1)
            {
                checkedSettingsCount++;
            }
            else if (addToScanResult)
            {
                decrypter?.DecryptSettingIfNeeded(profile.profileName, ref setting);
                checkedSettingsCount++;
                AddScannedSettingToCache(profile, setting);
                alreadyCheckedSettingIds.Add(setting.settingId);
                return (setting.isCurrentPredefined != 1);
            }
            else if (setting.isCurrentPredefined != 1)
            {
                return true;
            }

            return false;
        }

        private int CalcPercent(int current, int max)
        {
            return (current > 0) ? (int)Math.Round((current * 100f) / max) : 0;
        }

        public async Task ScanProfileSettingsAsync(bool justModified, IProgress<int> progress, CancellationToken token = default)
        {
            await Task.Run(() =>
            {
                ModifiedProfiles = new List<string>();
                UserProfiles = new HashSet<string>();
                var knownPredefines = new HashSet<uint>(_commonSettingIds);

                DrsSession((hSession) =>
                {
                    var profileHandles = EnumProfileHandles(hSession);
                    int maxProfileCount = profileHandles.Count;
                    int curProfilePos = 0;

                    foreach (IntPtr hProfile in profileHandles)
                    {
                        if (token.IsCancellationRequested) break;

                        progress?.Report(CalcPercent(curProfilePos++, maxProfileCount));

                        var profile = GetProfileInfo(hSession, hProfile);
                        int checkedSettingsCount = 0;
                        var alreadyChecked = new HashSet<uint>();

                        bool foundModifiedProfile = false;
                        if (profile.isPredefined == 0)
                        {
                            ModifiedProfiles.Add(profile.profileName);
                            UserProfiles.Add(profile.profileName);
                            foundModifiedProfile = true;
                            if (justModified) continue;
                        }

                        foreach (uint kpd in knownPredefines)
                        {
                            if (CheckCommonSetting(hSession, hProfile, profile,
                                ref checkedSettingsCount, kpd, !justModified, ref alreadyChecked))
                            {
                                if (!foundModifiedProfile)
                                {
                                    foundModifiedProfile = true;
                                    ModifiedProfiles.Add(profile.profileName);
                                    if (justModified) break;
                                }
                            }
                        }

                        if ((foundModifiedProfile && justModified) || checkedSettingsCount >= profile.numOfSettings)
                            continue;

                        var settings = GetProfileSettings(hSession, hProfile);
                        foreach (var setting in settings)
                        {
                            knownPredefines.Add(setting.settingId);

                            if (!justModified && !alreadyChecked.Contains(setting.settingId))
                                AddScannedSettingToCache(profile, setting);

                            if (setting.isCurrentPredefined != 1 && !foundModifiedProfile)
                            {
                                foundModifiedProfile = true;
                                ModifiedProfiles.Add(profile.profileName);
                                if (justModified) break;
                            }
                        }
                    }
                });
            }, token);
        }

        private void AddScannedSettingToCache(NVDRS_PROFILE profile, NVDRS_SETTING setting)
        {
            bool allowAddValue = !((setting.settingId & 0x70000000) == 0x70000000); // 3D Vision is dead

            var cachedSetting = CachedSettings
                .FirstOrDefault(x => x.SettingId.Equals(setting.settingId));

            bool cacheEntryExists = cachedSetting != null;
            if (!cacheEntryExists)
            {
                cachedSetting = new CachedSettings(setting.settingId, setting.settingType);
            }

            if (setting.isPredefinedValid == 1)
            {
                if (allowAddValue)
                {
                    switch (setting.settingType)
                    {
                        case NVDRS_SETTING_TYPE.NVDRS_WSTRING_TYPE:
                            cachedSetting.AddStringValue(setting.predefinedValue.stringValue, profile.profileName);
                            break;
                        case NVDRS_SETTING_TYPE.NVDRS_DWORD_TYPE:
                            cachedSetting.AddDwordValue(setting.predefinedValue.dwordValue, profile.profileName);
                            break;
                        case NVDRS_SETTING_TYPE.NVDRS_BINARY_TYPE:
                            cachedSetting.AddBinaryValue(setting.predefinedValue.binaryValue, profile.profileName);
                            break;
                    }
                }
                else
                {
                    cachedSetting.ProfileCount++;
                }

                if (!cacheEntryExists)
                {
                    CachedSettings.Add(cachedSetting);
                }
            }
        }

        public string FindProfilesUsingApplication(string applicationName)
        {
            string lowerApplicationName = applicationName.ToLowerInvariant();
            string tmpfile = TempFile.GetTempFileName();

            try
            {
                var matchingProfiles = new HashSet<string>();

                DrsSession((hSession) =>
                {
                    SaveSettingsFileEx(hSession, tmpfile);
                });

                if (File.Exists(tmpfile))
                {
                    string content = File.ReadAllText(tmpfile);
                    var profilePattern = new Regex(@"\sProfile\s\""(?<profile>.*?)\""(?<scope>.*?Executable.*?)EndProfile", RegexOptions.Singleline);
                    var executablePattern = new Regex(@"Executable\s\""(?<app>.*?)\""", RegexOptions.Singleline);

                    foreach (Match m in profilePattern.Matches(content))
                    {
                        string scope = m.Groups["scope"].Value;
                        foreach (Match ms in executablePattern.Matches(scope))
                        {
                            if (ms.Groups["app"].Value.ToLowerInvariant() == lowerApplicationName)
                            {
                                matchingProfiles.Add(m.Groups["profile"].Value);
                            }
                        }
                    }
                }

                return string.Join(";", matchingProfiles);
            }
            finally
            {
                if (File.Exists(tmpfile))
                    File.Delete(tmpfile);
            }
        }
    }
}