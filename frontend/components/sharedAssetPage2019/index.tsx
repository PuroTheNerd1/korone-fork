import React, { useEffect, useState } from "react";
import CatalogDetails from "../catalogDetailsPage";
import CatalogDetailsPage from "../catalogDetailsPage/stores/catalogDetailsPage";
import CatalogDetailsPageModal from "../catalogDetailsPage/stores/catalogDetailsPageModal";
import GameDetails from "../gameDetails";
import GameDetailsStore from "../gameDetails/stores/gameDetailsStore";
import { getItemDetailsNew, getItemUrl } from "../../services/catalog";
import {
  getGameUrl,
  getLibraryItemUrl,
  isLibraryItem,
} from "../../services/games";
import { useRouter } from "next/router";
import Feedback from "../../stores/feedback";
import ErrorPage from "../errorPage";
import AssetDetailsStore from "../AssetDetailsPage/stores/AssetDetailsStore";
import AssetDetailsPage from "../AssetDetailsPage";
import AssetDetailsModalStore from "../AssetDetailsPage/stores/AssetDetailsModalStore";
import { catalogPageStyle, getCatalogPageStyle } from "../../services/theme";
import SharedAssetPage from "../sharedAssetPage";
import { useSearchParams } from "next/navigation";

const getUrlForAssetType = (props: {
  assetTypeId: number;
  assetId: number;
  name: string;
}) => {
  if (props.assetTypeId === 9) {
    // Place
    return getGameUrl({
      placeId: props.assetId,
      name: props.name,
    });
  } else if (isLibraryItem({ assetTypeId: props.assetTypeId })) {
    // Library item
    return getLibraryItemUrl({
      assetId: props.assetId,
      name: props.name,
    });
  }
  // Anything else
  return getItemUrl({ assetId: props.assetId, name: props.name });
};

/**
 * @param {string} idParamName
 * @param {string} nameParamName
 * @param {ProductInfoLegacy} info
 * @returns {React.JSX.Element|null}
 * @constructor
 */
const AssetPage = async ({ idParamName, nameParamName = "" }) => {
  const router = useRouter();
  const params = useSearchParams();

  const [details, setDetails] = useState(null);
  const assetId = Number(params.get(idParamName));

  useEffect(() => {
    if (!assetId) return;

    if (!details || details.id !== assetId) {
      const newDetails = (async () => {
        return await getItemDetailsNew([assetId]);
      })().then((details) => {
        if (details.data.length > 0) {
          setDetails(newDetails[0]);

          const expectedUrl = getUrlForAssetType({
            assetTypeId: newDetails[0].assetType,
            assetId: newDetails[0].id,
            name: newDetails[0].name,
          });
          if (
            typeof window !== "undefined" &&
            window.location.href !== expectedUrl
          ) {
            router.push(expectedUrl).then();
          }
        } else {
          console.warn("New details length is 0, returning..");
        }
      });
    }
  }, [assetId]);

  if (getCatalogPageStyle() === catalogPageStyle.Legacy)
    return (
      <SharedAssetPage
        idParamName={idParamName}
        nameParamName={nameParamName}
      />
    );

  if (!details || !assetId)
    return (
      <span
        className="spinner"
        style={{ height: "100%", backgroundSize: "auto 36px" }}
      />
    );

  if (details.assetType === 9) {
    // Place
    return (
      <GameDetailsStore.Provider>
        <GameDetails details={details} />
      </GameDetailsStore.Provider>
    );
  }
  // Anything else (e.g. hat, shirt, model)
  return (
    <AssetDetailsStore.Provider>
      <AssetDetailsModalStore.Provider>
        <AssetDetailsPage details={details} />
      </AssetDetailsModalStore.Provider>
    </AssetDetailsStore.Provider>
  );
};

export default AssetPage;
