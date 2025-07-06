import {useRouter} from "next/router";
import ConfigureItem from "../../components/configureItem";
import { useSearchParams } from "next/navigation";

const MyItemPage = props => {
  const params = useSearchParams();

  return <ConfigureItem assetId={parseInt(params.get("id"),10)} />
}

export default MyItemPage;