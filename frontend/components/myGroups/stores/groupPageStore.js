import { useState } from "react";
import { createContainer } from "unstated-next";

const GroupPageStore = createContainer(() => {
  const [groupId, setGroupId] = useState(null);
  const [info, setInfo] = useState(null);
  const [rank, setRank] = useState(null);
  const [games, setGames] = useState(null);
  const [primary, setPrimary] = useState(null);
  const [previousNames, setPreviousNames] = useState(null);
  /**
   * @type {[Record<string, boolean>, import("react").Dispatch<Record<string, boolean>>]}
   */
  const [permissions, setPermissions] = useState({});

  return {
    groupId,
    setGroupId: (id) => {
      setInfo(null);
      setRank(null);
      setPreviousNames(null);
      setPermissions({})
      setGroupId(id);
    },

    info,
    setInfo,

    rank,
    setRank,

    permissions,
    setPermissions,

    games,
    setGames,

    primary,
    setPrimary,

    previousNames,
    setPreviousNames,
  }
});

export default GroupPageStore;