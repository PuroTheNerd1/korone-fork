import Head from 'next/head';
import Theme2016 from '../../../components/theme2016';
import GroupsPageStore from "../../../components/groupsNew/stores/GroupsPageStore";
import {GetServerSidePropsContext} from "next";
import {getInfo} from "../../../services/groups";
import GroupPreProcessor from "../../../components/groupsNew/GroupPreProcessor";
import {GroupWithShout} from "../../../services/groups-typed";
import React from "react";
import MyGroupsStore from "../../../components/myGroups/stores/myGroupsStore";
import { getGroupPagesStyle } from "../../../services/theme";
import GroupPageStore from "../../../components/myGroups/stores/groupPageStore";
import MyGroups from "../../../components/myGroups";

const GamePage = ({ group, groupId }: { group: GroupWithShout|null, groupId: any }) => {
    if (getGroupPagesStyle() !== 'Modern') return <MyGroupsStore.Provider>
        <GroupPageStore.Provider>
            <MyGroups id={groupId}/>
        </GroupPageStore.Provider>
    </MyGroupsStore.Provider>;

    return (
        <>
            {group !== null && (
                <Head>
                    <title>{group.name} - Korone</title>
                    <meta property="og:title" content={group.name} />
                    <meta property="og:url" content={`https://pekora.zip/games/${group.id}/--`} />
                    <meta property="og:type" content="profile" />
                    <meta property="og:description" content={group.description} />
                    <meta property="og:image" content={`https://pekora.zip/Thumbs/GameIcon.ashx?assetId=${group.id}`} />
                    <meta name="twitter:card" content="summary_large_image" />
                    <meta name="og:site_name" content="Korone" />
                    <meta name="theme-color" content="#E2231A" />
                </Head>
            )}
            <Theme2016>
                <GroupsPageStore.Provider>
                    <GroupPreProcessor group={group} />
                </GroupsPageStore.Provider>
            </Theme2016>
        </>
    );
}
export async function getServerSideProps(context: GetServerSidePropsContext) {
    const { groupId } = context.query;
    if (typeof groupId !== "string" || Number.isNaN(parseInt(groupId))) {
        console.error("Invalid groupId", groupId);
        return {
            props: {
                group: null,
                groupId: groupId,
            }
        };
    }

    const groupIdNum = parseInt(groupId);
    try {
        const info = await getInfo({groupId: groupIdNum});
        return {
            props: {
                group: info,
                groupId: groupId,
            }
        }
    } catch (error) {
        console.error(error);
        return {
            props: {
                group: null,
                groupId: groupId,
            }
        };
    }
}
export default GamePage;
