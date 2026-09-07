using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LanguageUpdater : MonoBehaviour
{
    public LanguageType currentLanguage = LanguageType.CHINESE;

    private LanguageType previousLanguage = LanguageType.CHINESE;

#if UNITY_EDITOR
    private void Update()
    {
        // if (currentLanguage != previousLanguage)
        // {
        //     previousLanguage = currentLanguage;
        //     CustomLocalization.SetLanguage(currentLanguage);
        // }
    }
#endif
}
