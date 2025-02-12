# **nvidiaProfileInspector-UNLOCKED**  

This repository contains unlocked settings for the main **NvidiaProfileInspector**, along with improvements in data loading, reading, and driver handling. Due to the number of new options, the standard program was not functioning properly, so this version was created.  

Some of the added options, such as **RTX HDR, DLSS, and Dynamic Vibrance**, were integrated thanks to the [nvidiaProfileInspectorForkAIO repository](https://github.com/neatchee/nvidiaProfileInspectorForkAIO). Additionally, this version incorporates the latest updates from the original **Orbmu2k** repository and its contributors.  

## **How Were These Values Obtained?**  

This project is based on an **Nvidia Leak**, which contains key data, including **HexSettingIDs**. Normally, these values would need to be manually added to `Reference.xml`, but in this case, they are already integrated into the program.  

This leak contains not only these settings but also a vast amount of additional valuable information.  

If you find any **errors, suggestions, or improvements**, feel free to contact me.  

## **Why Was This Created?**  

Simply to allow people to **learn and experiment** with hidden Nvidia settings.  

> **"Knowledge is power. But knowledge shared is power multiplied."** – Robert Boyce  

---

## **Configuration Structure**  

The configurations in this repository are based on Nvidia's driver leak, which allows access to hidden GPU options. These are stored in XML format and follow this structure:

### **XML Structure Overview**  

| XML Tag | Description |
|---------|------------|
| `<UserfriendlyName>` | The readable name of the setting. |
| `<HexSettingID>` | The unique hexadecimal ID used internally by Nvidia. |
| `<MinRequiredDriverVersion>` | Minimum driver version required for the setting to work. |
| `<GroupName>` | The category under which the setting appears (e.g., *Hidden Settings - [Nvidia Leak]*). |
| `<SettingValues>` | Contains multiple `<CustomSettingValue>` entries defining the possible values for the setting. |
| `<SettingMasks>` | *(Optional)* Additional parameters affecting how the setting is applied. |

### **Example of a Custom Setting**  

```xml
<CustomSetting>
  <UserfriendlyName>PS_COMPRESSION_FROMFILE</UserfriendlyName>
  <HexSettingID>0x00c96e68</HexSettingID>
  <MinRequiredDriverVersion>372.00</MinRequiredDriverVersion>
  <GroupName>Hidden Settings - [Nvidia Leak]</GroupName>
  <SettingValues>
    <CustomSettingValue>
      <UserfriendlyName>OFF</UserfriendlyName>
      <HexValue>0x00000000</HexValue>
    </CustomSettingValue>
    <CustomSettingValue>
      <UserfriendlyName>ON</UserfriendlyName>
      <HexValue>0x00000001</HexValue>
    </CustomSettingValue>
  </SettingValues>
</CustomSetting>
```

### **Available Setting Values**  

| Userfriendly Name | Hex Value | Meaning |
|------------------|----------|---------|
| OFF             | `0x00000000` | The setting is disabled. |
| ON              | `0x00000001` | The setting is enabled. |

> This allows users to experiment with hidden Nvidia settings to enhance GPU performance and customization.  
