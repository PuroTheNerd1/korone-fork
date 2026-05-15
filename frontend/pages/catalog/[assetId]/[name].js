import SharedAssetPage from "../../../components/sharedAssetPage2019";
import { getProductInfoLegacy } from "../../../services/catalog";
import { mintCheckoutPageTokenSsr } from "../../../services/economy";
import Head from "next/head";
import Theme2016 from "../../../components/theme2016";

const ItemPage = ({ name, description, assetId, checkoutPageToken, ...props }) => {
    return (
        <>
            {name && (
                <Head>
                    <title>{name} - Korone</title>
                    <meta property="og:title" content={name}/>
                    <meta property="og:url" content={`https://pekora.zip/catalog/${assetId}/--`}/>
                    <meta property="og:type" content="profile"/>
                    <meta property="og:description" content={description}/>
                    <meta property="og:image" content={`https://pekora.zip/thumbs/asset.ashx?assetId=${assetId}`}/>
                    <meta name="twitter:card" content="summary_large_image"/>
                    <meta name="og:site_name" content="Korone"/>
                    <meta name="theme-color" content="#E2231A"/>
                    {checkoutPageToken && (
                        <meta name="x-checkout-page-token" content={checkoutPageToken}/>
                    )}
                </Head>
            )}
            <SharedAssetPage idParamName='assetId' nameParamName='name'/>
        </>
    );
}

export async function getServerSideProps(context) {
    const { assetId } = context.query;
    const info = await getProductInfoLegacy(assetId);
    let checkoutPageToken = null;
    try {
        const fwd = context.req?.headers?.['x-forwarded-for'];
        const clientIp = (typeof fwd === 'string' ? fwd.split(',')[0].trim() : '')
            || context.req?.socket?.remoteAddress
            || context.req?.connection?.remoteAddress
            || '';
        checkoutPageToken = await mintCheckoutPageTokenSsr({
            assetId: Number(assetId),
            cookie: context.req?.headers?.cookie || '',
            clientIp,
        });
    } catch (e) {
        checkoutPageToken = null;
    }
    try {
        return {
            props: {
                name: info.Name,
                description: info.Description,
                assetId: assetId,
                checkoutPageToken,
            }
        };
    } catch (error) {
        console.error(error);
        return {
            props: {
                name: null,
                description: null,
                assetId: null,
                checkoutPageToken,
            }
        };
    }
}

export default ItemPage;