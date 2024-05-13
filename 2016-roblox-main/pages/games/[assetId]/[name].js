import { useRouter } from 'next/router';
import SharedAssetPage from "../../../components/sharedAssetPage";

const GamePage = () => {

  return (
    <>
      <SharedAssetPage idParamName='assetId' nameParamName='name' />
    </>
  );
}

export default GamePage;
