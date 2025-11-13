using System;
using System.Runtime.CompilerServices;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Events;

namespace Ducky.Sdk.GameApis;

// ReSharper disable once InconsistentNaming
public static class StockShopDatabaseExtensions
{
    // Instance field accessors
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static List<StockShopDatabase.MerchantProfile> GetMerchantProfiles(this StockShopDatabase target) =>
        target.GetField<StockShopDatabase, List<StockShopDatabase.MerchantProfile>>("merchantProfiles");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StockShopDatabase SetMerchantProfiles(this StockShopDatabase target, List<StockShopDatabase.MerchantProfile> value) =>
        target.SetField("merchantProfiles", value);

}
