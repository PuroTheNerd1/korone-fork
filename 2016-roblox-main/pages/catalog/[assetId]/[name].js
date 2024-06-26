import SharedAssetPage from "../../../components/sharedAssetPage";
import { useRouter } from 'next/router';
import { getItemDetails } from "../../../services/catalog";
import { useState } from "react";
import Head from "next/head";
const ItemPage = ({ name, description, assetId, ...props }) => {
  return(
    <>
      {name && (
        <Head>
          <title>{name} - Project X</title>
          <meta property="og:title" content={name} />
          <meta property="og:url" content={`https://www.projex.zip/catalog/${assetId}/--`} />
          <meta property="og:type" content="profile" />
          <meta property="og:description" content={description} />
          <meta property="og:image" content={`https://www.projex.zip/thumbs/asset.ashx?userId=${assetId}`} />
          <meta name="twitter:card" content="summary_large_image" />
          <meta name="og:site_name" content="Project X" />
          <meta name="theme-color" content="#f00000" />
        </Head>
      )}
      <SharedAssetPage idParamName='assetId' nameParamName='name' />
    </>
  ); 
}

export async function getServerSideProps(context) {
  const { assetId } = context.query;

  try {
    const result = await getItemDetails([assetId]);
    const newDetails = result.data.data[0];
    if (newDetails === undefined) {
      throw new Error('NotFound');
    }

    return {
      props: {
        name: newDetails.name,
        description: newDetails.description,
        assetId: assetId
      }
    };
  } catch (error) {
    return {
      name: null,
      description: null,
      assetId: null
    };
  }
}
export default ItemPage;