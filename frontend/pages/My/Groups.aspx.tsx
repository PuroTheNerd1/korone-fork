import React from "react";
import MyGroups from "../../components/myGroups";
import GroupPageStore from "../../components/myGroups/stores/groupPageStore";
import MyGroupsStore from "../../components/myGroups/stores/myGroupsStore";
import { useSearchParams } from "next/navigation";

const MyGroupsPage = props => {
  const params = useSearchParams();
  return <MyGroupsStore.Provider>
    <GroupPageStore.Provider>
      <MyGroups id={params.get('gid')}/>
    </GroupPageStore.Provider>
  </MyGroupsStore.Provider>
}

export default MyGroupsPage;
