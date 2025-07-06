import SearchGroups from "../../components/searchGroups";
import {useRouter} from "next/router";

const SearchGroupsPage = props => {
  const router = useRouter();
  return <SearchGroups keyword={router.query.keyword} />
}

export default SearchGroupsPage;