using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;

// Unity IAP v4 API 사용(v5에서도 동작). v5 마이그레이션 전까지 사용 중단(deprecated) 경고를 끕니다.
#pragma warning disable 618

/// <summary>
/// Unity IAP(Google Play Billing) 기반 결제 서비스.
/// - 신규 구매: Purchase() → ProcessPurchase 성공 시 콜백(true) → UI가 지급
/// - 복원: 앱 시작 시 Google Play가 보유한 비소모성(광고 제거)을 ProcessPurchase로 전달 → onRestoreOwned 호출
/// 소모성(크리스탈)은 지급 후 자동 소모되어 재구매 가능합니다.
/// </summary>
public sealed class GooglePlayStoreService : IStorePurchaseService, IStoreListener
{
    public struct ProductInfo
    {
        public string id;
        public bool nonConsumable; // true = 광고 제거처럼 영구 보유
    }

    IStoreController m_controller;
    bool m_initialized;
    readonly Dictionary<string, Action<bool>> m_pending = new Dictionary<string, Action<bool>>();
    readonly Action<string> m_onRestoreOwned;

    public GooglePlayStoreService(IEnumerable<ProductInfo> products, Action<string> onRestoreOwned)
    {
        m_onRestoreOwned = onRestoreOwned;

        ConfigurationBuilder builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
        foreach (ProductInfo info in products)
        {
            if (string.IsNullOrEmpty(info.id)) continue;
            builder.AddProduct(info.id, info.nonConsumable ? ProductType.NonConsumable : ProductType.Consumable);
        }
        UnityPurchasing.Initialize(this, builder);
    }

    public void Purchase(string productId, Action<bool> completed)
    {
        if (!m_initialized || m_controller == null || string.IsNullOrEmpty(productId))
        {
            completed?.Invoke(false);
            return;
        }

        Product product = m_controller.products.WithID(productId);
        if (product == null || !product.availableToPurchase)
        {
            completed?.Invoke(false);
            return;
        }

        m_pending[productId] = completed;
        m_controller.InitiatePurchase(product);
    }

    public string GetLocalizedPrice(string productId)
    {
        if (!m_initialized || m_controller == null || string.IsNullOrEmpty(productId)) return null;
        Product product = m_controller.products.WithID(productId);
        return product != null ? product.metadata.localizedPriceString : null;
    }

    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        m_controller = controller;
        m_initialized = true;
    }

    public void OnInitializeFailed(InitializationFailureReason error)
        => Debug.LogWarning($"[IAP] init failed: {error}");

    public void OnInitializeFailed(InitializationFailureReason error, string message)
        => Debug.LogWarning($"[IAP] init failed: {error} ({message})");

    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    {
        string id = args.purchasedProduct.definition.id;
        if (m_pending.TryGetValue(id, out Action<bool> callback))
        {
            m_pending.Remove(id);
            callback?.Invoke(true);
        }
        else
        {
            // 앱 시작 시 복원된 비소모성(예: 광고 제거)
            m_onRestoreOwned?.Invoke(id);
        }
        return PurchaseProcessingResult.Complete;
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureReason reason)
    {
        string id = product != null ? product.definition.id : null;
        if (id != null && m_pending.TryGetValue(id, out Action<bool> callback))
        {
            m_pending.Remove(id);
            callback?.Invoke(false);
        }
        Debug.LogWarning($"[IAP] purchase failed: {id} / {reason}");
    }
}
#pragma warning restore 618
