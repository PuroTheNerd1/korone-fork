import {createContainer} from "unstated-next";
import {useState} from "react";

const GroupsPageStore = createContainer(() => {
    const [groups, setGroups] = useState<string[]>([]);
    const [icons, setIcons] = useState<string[]>([]);
    const [primary, setPrimaryGroup] = useState<string[]>([]);

    return {
        groups, setGroups,
        icons, setIcons,
    }
});

export default GroupsPageStore;