import { createContainer } from "unstated-next";
import { useEffect, useState } from "react";
import {
  getResaleData,
  getResellableCopies,
  getResellers,
} from "../../../services/economy";
import {
  multiGetUserHeadshots,
  multiGetUserHeadshots2,
  multiGetUserThumbnails,
} from "../../../services/thumbnails";
import {
  getCollectibleOwners,
  getCollections,
  getOwnedCopies,
  userOwnsItem,
} from "../../../services/inventory";
import Authentication from "../../../stores/authentication";
import { CurrencyType } from "../../../models/enums";
import FeedbackStore from "../../../stores/feedback";
import { addOrRemoveFromCollections } from "../../../services/catalog";
import { FeedbackType } from "../../../models/feedback";

const assetDetailsStore = createContainer(() => {
  const auth = Authentication.useContainer();
  const feedback = FeedbackStore.useContainer();
  const [details, setDetails] = useState<AssetDetailsEntry | null>(null);
  const [resellers, setResellers] = useState<ResellerDataThumb[]>([]);
  const [owners, setOwners] = useState<OwnerEntryThumb[]>([]);
  const [ownedCopies, setOwnedCopies] = useState([]);
  const [isCollectioned, setCollectioned] = useState(false);
  const [isOwned, setOwned] = useState(false);
  // TODO: implement "Wear" button in dropdown
  // const [isEquipped, setEquipped] = useState(false);
  const [resaleData, setResaleData] = useState(null);

  /**
   * @param {boolean} isCollected
   * @returns {Promise<void>}
   * @constructor
   */
  async function ToggleFromCollection(isCollected) {
    try {
      await addOrRemoveFromCollections({
        assetId: details.id,
        addToProfile: isCollected,
      });
      feedback.addFeedback(
        (isCollected ? "Added to" : "Removed from") + " your collection"
      );
    } catch (e) {
      feedback.addFeedback(e.message, FeedbackType.ERROR);
    }
  }

  useEffect(() => {
    (async () => {
      if (!details) return;

      userOwnsItem({ userId: auth.userId, assetId: details.id }).then(setOwned);
      /** @type number[] */
      getCollections({ userId: auth.userId }).then((d) =>
        setCollectioned(d.map((d) => d.Id).includes(details.id))
      );

      if (isResellable()) {
        getResellableCopies({ assetId: details.id, userId: auth.userId }).then(
          (d) => setOwnedCopies(d?.data || [])
        );
      } else {
        getOwnedCopies({ assetId: details.id, userId: auth.userId }).then((d) =>
          setOwnedCopies(d || [])
        );
      }

      if (!isLimited()) return;
      getResaleData({ assetId: details.id }).then(setResaleData);

      if (!isResellable()) return;
      loadResellers();
      loadOwners();
    })();
  }, [details]);

  async function loadResellers() {
    let data = [];
    let cursor = "";
    // might have to be do while instead of while
    while (cursor !== null) {
      /** @type PekoraCollectionPaginated<ResellerData> */
      const resellData = (
        await getResellers({ assetId: details.id, cursor: cursor, limit: 100 })
      ).data;
      if (!resellData || resellData.data.length === 0) {
        cursor = null;
        break;
      }
      const resellThumbs = await multiGetUserHeadshots2({
        userIds: resellData.data.map((d) => d.seller.id),
      });
      resellData.data.forEach((seller) => {
        let thumb = resellThumbs.find((d) => d.targetId === seller.seller.id);
        data.push({
          ...seller,
          imageUrl: thumb?.imageUrl || "",
          state: thumb?.state || "",
        });
      });
      cursor = resellData.nextPageCursor;
    }
    setResellers(data);
  }
  async function loadOwners() {
    let data = [];
    let cursor = "";
    // might have to be do while instead of while
    while (cursor !== null) {
      /** @type PekoraCollectionPaginated<OwnerEntry> */
      const ownerData = await getCollectibleOwners({
        assetId: details.id,
        cursor: cursor,
        limit: 50,
        sort: "Asc",
      });
      if (ownerData.data.length === 0) {
        cursor = null;
        break;
      }
      const ownerThumbs = await multiGetUserHeadshots2({
        userIds: ownerData.data
          .filter((d) => d.owner?.id)
          .map((d) => d.owner.id),
      });
      ownerData.data.forEach((owner) => {
        let thumb = ownerThumbs.find(
          (d) => d.targetId === (owner?.owner?.id || 0)
        );
        data.push({
          ...owner,
          imageUrl: thumb?.imageUrl || "",
          state: thumb?.state || "",
        });
      });
      cursor = ownerData.nextPageCursor;
    }
    setOwners(data);
  }

  // is it limited BUT not all copies have been bought?
  /** @returns {boolean} */
  const isLimited = () => {
    return (
      details?.itemRestrictions &&
      (details.itemRestrictions.includes("Limited") ||
        details.itemRestrictions.includes("LimitedUnique"))
    );
  };
  // is it limited but all copies are bought?
  /** @returns {boolean} */
  const isResellable = () => {
    return isLimited() && !details.isForSale;
  };
  /**
   * @returns {CurrencyType}
   */
  const getCurrency = () => {
    if (!details) return CurrencyType.Robux;
    if (!details.price && details.priceTickets) return CurrencyType.Tickets;
    return CurrencyType.Robux;
  };
  /**
   * @param {number|null} price
   * @param {number|null} priceTickets
   * @returns {number}
   */
  const GetCurrencyFromPrices = (price, priceTickets) => {
    if (!price || !priceTickets) return CurrencyType.Robux;
    if (!price && priceTickets) return CurrencyType.Tickets;
    return CurrencyType.Robux;
  };

  /**
   * @param {number|null} [specificUAID]
   * @returns {PurchaseDetails|null}
   */
  const getPurchaseInfo = (specificUAID = null) => {
    if (!details) return null;
    if (isResellable()) {
      const seller = specificUAID
        ? resellers.find((d) => d.userAssetId === specificUAID)
        : resellers.length > 0 && resellers[0];
      if (!seller) return null;
      return {
        assetId: details.id,
        sellerName: seller.seller.name,
        sellerId: seller.seller.id,
        price: seller.price,
        priceTickets: null,
        userAssetId: seller.userAssetId,
        productId: details.productId || details.id,
        currency: getCurrency(),
        expectedPrice: seller.price,
      };
    } else if (details.isForSale) {
      return {
        assetId: details.id,
        sellerName: details.creatorName,
        sellerId: details.creatorTargetId,
        price: details.price,
        priceTickets: details.priceTickets,
        userAssetId: null,
        productId: details.productId || details.id,
        currency: getCurrency(),
        expectedPrice:
          getCurrency() === CurrencyType.Tickets
            ? details.priceTickets
            : details.price,
      };
    }
    return null;
  };

  return {
    ToggleFromCollection,
    GetCurrencyFromPrices,

    /** @type AssetDetailsEntry */
    details,
    setDetails,

    /** @type ResellerDataThumb[] */
    resellers,
    /** @type OwnerEntryThumb[] */
    owners,
    /** @type boolean */
    isCollectioned,
    setCollectioned,
    /** @type ResellerData[] */
    ownedCopies,
    isOwned,
    /** @type ResaleData */
    resaleData,

    /** @returns {boolean} */
    isLimited,
    /** @returns {boolean} */
    isResellable,
    getPurchaseInfo,
  };
});

export default assetDetailsStore;

/**
 * @typedef ResellerData
 * @property {number} userAssetId
 * @property {number} [seller.id]
 * @property {string} [seller.type]
 * @property {string} [seller.name]
 * @property {number} [price]
 * @property {number} [serialNumber]
 */

interface ResellerData {
  userAssetId: number;
  seller: {
    id: number;
    name: string;
    type: string;
  };
  price: number;
  serialNumber: number;
}

/**
 * @typedef ResellerDataThumb
 * @property {number} userAssetId
 * @property {number} seller.id
 * @property {string} seller.type
 * @property {string} seller.name
 * @property {number} price
 * @property {number} serialNumber
 * @property {string} state
 * @property {string} imageUrl
 */

interface ResellerDataThumb {
  userAssetId: number;
  seller: {
    id: number;
    type: string;
    name: string;
  };
  price: number;
  serialNumber: number;
  state: string;
  imageUrl: string;
}

/**
 * @typedef ResaleData
 * @property {number} sales
 * @property {number} numberRemaining
 * @property {number} recentAveragePrice
 * @property {{ value: number; date: string; }[]} priceDataPoints
 * @property {{ value: number; date: string; }[]} volumeDataPoints
 */

interface ResaleData {
  sales: number;
  numberRemaining: number;
  recentAveragePrice: number;
  priceDataPoints: {
    value: number;
    date: string;
  }[];
}

/**
 * @typedef PurchaseDetails
 * @property {number} assetId
 * @property {string} sellerName
 * @property {number} sellerId
 * @property {number|null} price
 * @property {number|null} priceTickets
 * @property {number|null} userAssetId
 * @property {number} productId
 * @property {number} currency
 * @property {number} expectedPrice
 */

interface PurchaseDetails {
  assetId: number;
  sellerName: string;
  sellerId: number;
  price: number | null;
  priceTickets: number | null;
  userAssetId: number;
  productId: number;
  currency: number;
  expectedPrice: number;
}

/**
 * @typedef OwnerEntry
 * @property {null|{ id: number; type: string; name: string; }} owner
 * @property {number} id UAID
 * @property {number} serialNumber
 * @property {string} created
 * @property {string} updated
 */

interface OwnerEntry {
  owner: { id: number; type: string; name: string } | null;
  id: number;
  serialNumber: number;
  created: string;
  updated: string;
}

/**
 * @typedef OwnerEntryThumb
 * @property {null|{ id: number; type: string; name: string; }} owner
 * @property {number} id UAID
 * @property {number} serialNumber
 * @property {string} created
 * @property {string} updated
 * @property {string} imageUrl
 * @property {string} state
 */

interface OwnerEntryThumb {
  owner: { id: number; type: string; name: string } | null;
  id: number;
  serialNumber: number;
  created: string;
  updated: string;
  imageUrl: string;
  state: string;
}
