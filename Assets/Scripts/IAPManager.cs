using System;
using UnityEngine;

#if USE_UNITY_IAP
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Security;

public class IAPManager : MonoBehaviour, IStoreListener
{
    public static IAPManager Instance;
    private static IStoreController m_StoreController;          // The Unity Purchasing system.
    private static IExtensionProvider m_StoreExtensionProvider; // The store-specific Purchasing subsystems.
    private Action<string, bool, PurchaseFailureReason> PurchaserManager_Callback = delegate (string _iapID, bool _callBackState, PurchaseFailureReason reason) { };

    // public string 
    public string gem1 = "gem1";
    public string gem2 = "gem2";
    public string gem3 = "gem3";
    public string gem4 = "gem4";

    public string bundle_1 = "bundle_1";
    public string bundle_2 = "bundle_2";
    public string bundle_3 = "bundle_3";
    public string gemshop_1 = "gemshop_1";
    public string gemshop_2 = "gemshop_2";

    public string starter_pack = "starter_pack";
    public string weekly_pack = "weekly_pack";
    public string monthly_pack = "monthly_pack";

    public string piggy_bank_1 = "piggy_bank_1";
    public string piggy_bank_2 = "piggy_bank_2";
    public string piggy_bank_3 = "piggy_bank_3";
    public string piggy_bank_4 = "piggy_bank_4";
    public string piggy_bank_5 = "piggy_bank_5";

    public string no_ads = "no_ads";

    void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(this);
    }

    void Start()
    {
        if (m_StoreController == null)
        {
            InitializePurchasing();
        }
    }

    public bool IsInitialized()
    {
#if UNITY_EDITOR
        return true;
#endif
        return m_StoreController != null && m_StoreExtensionProvider != null;
    }

    public void InitializePurchasing()
    {
        if (IsInitialized())
        {
            return;
        }

        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

        builder.AddProduct(this.gem1, ProductType.Consumable);
        builder.AddProduct(this.gem2, ProductType.Consumable);
        builder.AddProduct(this.gem3, ProductType.Consumable);
        builder.AddProduct(this.gem4, ProductType.Consumable);

        builder.AddProduct(this.bundle_1, ProductType.Consumable);
        builder.AddProduct(this.bundle_2, ProductType.Consumable);
        builder.AddProduct(this.bundle_3, ProductType.Consumable);
        builder.AddProduct(this.gemshop_1, ProductType.Consumable);
        builder.AddProduct(this.gemshop_2, ProductType.Consumable);

        builder.AddProduct(this.starter_pack, ProductType.NonConsumable);
        builder.AddProduct(this.weekly_pack, ProductType.NonConsumable);
        builder.AddProduct(this.monthly_pack, ProductType.NonConsumable);

        builder.AddProduct(this.piggy_bank_1, ProductType.Consumable);
        builder.AddProduct(this.piggy_bank_2, ProductType.Consumable);
        builder.AddProduct(this.piggy_bank_3, ProductType.Consumable);
        builder.AddProduct(this.piggy_bank_4, ProductType.Consumable);
        builder.AddProduct(this.piggy_bank_5, ProductType.Consumable);

        builder.AddProduct("no_ads", ProductType.NonConsumable);

        UnityPurchasing.Initialize(this, builder);
    }

    public void BuyConsumable(string iapID, Action<string, bool, PurchaseFailureReason> _purchaserManager_Callback)
    {
        PurchaserManager_Callback = _purchaserManager_Callback;
        BuyProductID(iapID);
    }

    void BuyProductID(string productId)
    {
#if UNITY_EDITOR
        PurchaserManager_Callback.Invoke(productId, true, PurchaseFailureReason.Unknown);
#else
        if (IsInitialized())
        {
            Product product = m_StoreController.products.WithID(productId);

            if (product != null && product.availableToPurchase)
            {
                m_StoreController.InitiatePurchase(product);
            }
            else
            {
                PurchaserManager_Callback.Invoke(productId, false, PurchaseFailureReason.Unknown);
            }
        }
        else
        {
            PurchaserManager_Callback.Invoke(productId, false, PurchaseFailureReason.Unknown);
        }
#endif
    }

    public void RestorePurchases()
    {
        if (!IsInitialized())
        {
            return;
        }

        if (Application.platform == RuntimePlatform.IPhonePlayer ||
            Application.platform == RuntimePlatform.OSXPlayer)
        {
            var apple = m_StoreExtensionProvider.GetExtension<IAppleExtensions>();
            apple.RestoreTransactions((result) => {
                Debug.Log("RestorePurchases continuing: " + result + ". If no further messages, no purchases available to restore.");
            });
        }
        else
        {
            Debug.Log("RestorePurchases FAIL. Not supported on this platform. Current = " + Application.platform);
        }
    }

    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        m_StoreController = controller;
        m_StoreExtensionProvider = extensions;
    }

    // 实现第一个版本的 OnInitializeFailed 方法
    public void OnInitializeFailed(InitializationFailureReason error)
    {
        Debug.Log("OnInitializeFailed InitializationFailureReason:" + error);
    }

    // 修正后的方法签名 - 添加了string参数
    public void OnInitializeFailed(InitializationFailureReason error, string message)
    {
        Debug.Log("OnInitializeFailed InitializationFailureReason:" + error + ", Message: " + message);
    }

    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs purchaseEvent)
    {
        bool validPurchase = true;
#if UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE_OSX
        var validator = new CrossPlatformValidator(GooglePlayTangle.Data(),
            AppleTangle.Data(), Application.identifier);
        try
        {
            var result = validator.Validate(purchaseEvent.purchasedProduct.receipt);
            Debug.Log("Receipt is valid. Contents:");
            foreach (IPurchaseReceipt productReceipt in result)
            {
                Debug.Log(productReceipt.productID);
                Debug.Log(productReceipt.purchaseDate);
                Debug.Log(productReceipt.transactionID);
            }
        }
        catch (IAPSecurityException)
        {
            Debug.Log("Invalid receipt, not unlocking content");
            validPurchase = false;
        }
#endif

        if (validPurchase)
        {
            PurchaserManager_Callback.Invoke(purchaseEvent.purchasedProduct.definition.id, true, PurchaseFailureReason.Unknown);
        }
        else
        {
            PurchaserManager_Callback.Invoke(purchaseEvent.purchasedProduct.definition.id, false, PurchaseFailureReason.Unknown);
        }

        return PurchaseProcessingResult.Complete;
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
    {
        Debug.Log(string.Format("OnPurchaseFailed: FAIL. Product: '{0}', PurchaseFailureReason: {1}", product.definition.storeSpecificId, failureReason));
        PurchaserManager_Callback.Invoke(product.definition.id, false, failureReason);
    }
}
#else
// Unity IAP is intentionally disabled.
// To restore it, install com.unity.purchasing and add USE_UNITY_IAP to Player Settings > Scripting Define Symbols.
public enum PurchaseFailureReason
{
    PurchasingUnavailable
}

public class IAPManager : MonoBehaviour
{
    public static IAPManager Instance;

    // Keep the product IDs serialized in existing scenes and prefabs for a future IAP implementation.
    public string gem1 = "gem1";
    public string gem2 = "gem2";
    public string gem3 = "gem3";
    public string gem4 = "gem4";

    public string bundle_1 = "bundle_1";
    public string bundle_2 = "bundle_2";
    public string bundle_3 = "bundle_3";
    public string gemshop_1 = "gemshop_1";
    public string gemshop_2 = "gemshop_2";

    public string starter_pack = "starter_pack";
    public string weekly_pack = "weekly_pack";
    public string monthly_pack = "monthly_pack";

    public string piggy_bank_1 = "piggy_bank_1";
    public string piggy_bank_2 = "piggy_bank_2";
    public string piggy_bank_3 = "piggy_bank_3";
    public string piggy_bank_4 = "piggy_bank_4";
    public string piggy_bank_5 = "piggy_bank_5";

    public string no_ads = "no_ads";

    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(this);
    }

    public bool IsInitialized()
    {
        return false;
    }

    public void InitializePurchasing()
    {
        // Intentionally empty while the purchasing package is not part of the project.
    }

    public void BuyConsumable(string iapID, Action<string, bool, PurchaseFailureReason> callback)
    {
        Debug.LogWarning("Purchase ignored because Unity IAP is disabled. Product: " + iapID);
        callback?.Invoke(iapID, false, PurchaseFailureReason.PurchasingUnavailable);
    }

    public void RestorePurchases()
    {
        Debug.LogWarning("Restore purchases ignored because Unity IAP is disabled.");
    }
}
#endif
