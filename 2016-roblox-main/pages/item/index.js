import Head from "next/head";
import { getProductInfoLegacy } from "../../services/catalog";
import SharedAssetPage2019 from "../../components/sharedAssetPage2019";
import Theme2016 from "../../components/theme2016";

const ItemPage = ({ assetId, info, ...props }) => {
    return <Theme2016>
        {info && (
            <Head>
                <title>{info.name} - Pekora</title>
                <meta property="og:title" content={info.name}/>
                <meta property="og:url" content={`https://goober.top/item?assetId=${assetId}`}/>
                <meta property="og:type" content="profile"/>
                <meta property="og:description" content={info.description}/>
                <meta property="og:image" content={`https://goober.top/thumbs/asset.ashx?assetId=${assetId}`}/>
                <meta name="twitter:card" content="summary_large_image"/>
                <meta name="og:site_name" content="Pekora"/>
                <meta name="theme-color" content="#E2231A"/>
            </Head>
        )}
        <SharedAssetPage2019 itemDetails={info} idParamName="assetId" />
    </Theme2016>
}

export async function getServerSideProps(context) {
    const { assetId } = context.query;
    const info = await getProductInfoLegacy(assetId);
    try {
        if (!info) throw new Error("Asset does not exist.");
        return {
            props: {
                assetId: assetId,
                info: info,
            }
        };
    } catch (error) {
        console.error(error);
        return {
            props: {
                assetId: null,
                info: null,
            }
        };
    }
}

export default ItemPage;