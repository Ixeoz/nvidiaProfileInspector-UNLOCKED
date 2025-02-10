using System;
using System.IO;
using System.Reflection;
using nspector.Common.CustomSettings;

namespace nspector.Common
{
    internal class DrsServiceLocator
    {
        private static readonly CustomSettingNames CustomSettings;
        public static readonly CustomSettingNames ReferenceSettings;
        public static readonly DrsSettingsMetaService MetaService;
        public static readonly DrsSettingsService SettingService;
        public static readonly DrsImportService ImportService;
        public static readonly DrsScannerService ScannerService;
        public static readonly DrsDecrypterService DecrypterService;

        public static bool IsExternalCustomSettings { get; private set; } = false;

        static DrsServiceLocator()
        {
            CustomSettings = LoadCustomSettings();
            ReferenceSettings = LoadReferenceSettings();

            MetaService = new DrsSettingsMetaService(CustomSettings, ReferenceSettings);
            DecrypterService = new DrsDecrypterService(MetaService);
            ScannerService = new DrsScannerService(MetaService, DecrypterService);
            SettingService = new DrsSettingsService(MetaService, DecrypterService);
            ImportService = new DrsImportService(MetaService, SettingService, ScannerService, DecrypterService);
        }

        private static CustomSettingNames LoadCustomSettings()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = "nspector.Resources.CustomSettingNames.xml";

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                        throw new FileNotFoundException($"Resource not found: {resourceName}");

                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string xmlContent = reader.ReadToEnd();
                        return CustomSettingNames.FactoryLoadFromString(xmlContent);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading CustomSettingNames.xml: {ex.Message}");
                return CustomSettingNames.FactoryLoadFromString(Properties.Resources.CustomSettingNames);
            }
        }

        private static CustomSettingNames LoadReferenceSettings()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = "nspector.Resources.Reference.xml";

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                        throw new FileNotFoundException($"Resource not found: {resourceName}");

                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string xmlContent = reader.ReadToEnd();
                        return CustomSettingNames.FactoryLoadFromString(xmlContent);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading Reference.xml: {ex.Message}");
                return null;
            }
        }
    }
}