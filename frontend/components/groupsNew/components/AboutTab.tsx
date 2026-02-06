import {createUseStyles} from 'react-jss';
import {useState, useEffect} from 'react';
import Section from "./Section";
import GroupsPageStore from "../stores/GroupsPageStore";
import {GroupUserWithRoleIdThumbnail} from "../../../services/groups-typed";
import NewLink from "../../NewLink";
import {ThumbnailFromState} from "../../AvatarEditorPage/components/avatarCardList";

const useStyles = createUseStyles({
    description: {
        color: "var(--text-color-primary)",
        fontSize: 16,
        wordWrap: "break-word",
        fontFamily: "HCo Gotham SSm,Helvetica Neue,Helvetica,Arial,Lucida Grande,sans-serif",
        fontWeight: 400,
        lineHeight: "1.4em",
        whiteSpace: "pre-wrap",
        paddingBottom: 12,
        textRendering: "auto",
    },
    memberWrapper: {
        float: "left",
        width: "11.1111111%",
        height: "120px",
        minWidth: "100%",
        listStyle: "none",
    },
    memberLink: {
        width: "90px",
        margin: "auto",
        textDecoration: 'none',
    },
    avatarContainer: {
        boxShadow: '0 1px 4px 0 rgba(25,25,25,0.3)',
        transition: "box-shadow 200ms ease",
        borderRadius: "50%",
        backgroundColor: "#d1d1d1",
        overflow: "hidden",
        "& img": {
            objectFit: "cover",
            background: 'none',
            verticalAlign: "bottom",
        },
        "&:hover": {
            boxShadow: "0 1px 6px 0 rgba(25, 25, 25, 0.75)",
        },
    },
    userName: {
        margin: "3px 0 0",
        lineHeight: "1.867em",
        textOverflow: "ellipsis",
        fontWeight: 500,
    },
});

// have: description, games (should have nothing), members, social links
const AboutTab = ({}: {}) => {
    const s = useStyles();
    const store = GroupsPageStore.useContainer();
    const {members} = GroupsPageStore.useContainer();

    return <div>
        <Section header={"Description"} contentSectioned={true}>
            <pre className={`${s.description} w-100 m-0 overflow-hidden `}>{store.group.description}</pre>
        </Section>
        <Section header={"Games"} contentSectioned={true} className={"disabled"}>
            This group has not created any games yet.
        </Section>
        <Section header={"Members"} contentSectioned={true} className={members.members.length === 0 ? "disabled" : ""}
                 headerChildren={<>
                     {/*take from catlaog page*/}
                     <div className={`${s.pageControls}`}></div>
                     {/*just use selector class*/}
                     <div className={`${s.roleSelector}`}></div>
                 </>}
        >
            {members.members.length === 0 ? "This role has no members." : <ul>
                {members.members.map(m => MemberItem({member: m}))}
            </ul>}
        </Section>
        <Section header={"Social Links"} contentSectioned={true}>
            <pre className={`${s.description} w-100 m-0 overflow-hidden `}>{store.group.description}</pre>
        </Section>
    </div>
};

const MemberItem = ({member}: { member: GroupUserWithRoleIdThumbnail }) => {
    const s = useStyles();
    // NOTE: newlink is an example of how to solve jsdoc-ts issues
    return <li className={`${s.memberWrapper}`}>
        <NewLink href={`/users/${member.userId}/profile`} className={s.memberLink}>
            <span className={`${s.avatarContainer} w-100 border-0 h-100 text-center`}>
                <img
                    className={`rounded-circle`}
                    alt={`Headshot of ${member.displayName}'s Avatar`}
                    src={ThumbnailFromState(member.imageUrl, member.state)}
                />
            </span>
            <span className={`${s.userName} link2019 overflow-hidden font-size-12 text-center text-nowrap text-decoration-none`}>{member.displayName}</span>
        </NewLink>
    </li>
}

export default AboutTab;