import SearchUsers from "../../components/searchUsers";
import SearchUsersNew from "../../components/searchUsersNew";
import Theme2016 from "../../components/theme2016";
import { useRouter } from "next/dist/client/router";
import { useEffect, useState } from "react";
import { getSearchUserPageStyle, searchUserPageStyle } from "../../services/theme";

const SearchUsersPage = () => {
  const router = useRouter();
  const [style, setStyle] = useState(/** @type {string|null} */(null));

  useEffect(() => {
    setStyle(getSearchUserPageStyle());
  }, []);

  if (style === null) return null;
  if (style === searchUserPageStyle.Modern)
    return <Theme2016>
      <SearchUsersNew keyword={router.query.keyword} />
    </Theme2016>;
  return <SearchUsers keyword={router.query.keyword} />;
};

export default SearchUsersPage;
