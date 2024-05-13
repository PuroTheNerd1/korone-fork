import { useRouter } from 'next/router';
import SharedAssetPage from "../../../components/sharedAssetPage";
import Head from 'next/head';

const GamePage = () => {
  const router = useRouter();
  const { name, assetId } = router.query;

  return (
    <>
      <Head>
        <meta property="og:title" content={`${name}`} />
        <meta property="og:url" content={`https://www.projex.zip/games/${assetId}/${name}`} />
        <meta property="og:type" content="profile" />
        <meta property="og:image" content={`https://www.projex.zip/Thumbs/Asset.ashx?assetId=${assetId}`} />
        <meta name="theme-color" content="#f00000" />
      </Head>
      <SharedAssetPage idParamName='assetId' nameParamName='name' />
    </>
  );
}

export default GamePage;
