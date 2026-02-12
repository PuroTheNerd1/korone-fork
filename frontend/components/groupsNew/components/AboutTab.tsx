import {createUseStyles} from 'react-jss';
import {useState, useEffect, useRef} from 'react';
import Section from "./Section";
import GroupsPageStore from "../stores/GroupsPageStore";
import {GroupUserWithRoleIdThumbnail} from "../../../services/groups-typed";
import NewLink from "../../NewLink";
import {ThumbnailFromState} from "../../AvatarEditorPage/components/avatarCardList";
import CreatorLink from "../../creatorLink";
import dayjs from "../../../lib/dayjs";
import PlayerHeadshot from "../../playerHeadshot";
import useButtonStyles from "../../../styles/buttonStyles";
import ActionButton from "../../actionButton";

const useStyles = createUseStyles({
    description: {
        color: "var(--text-color-primary)",
        fontSize: 16,
        wordWrap: "break-word",
        fontFamily: "HCo Gotham SSm,Helvetica Neue,Helvetica,Arial,Lucida Grande,sans-serif",
        fontWeight: 400,
        lineHeight: "1.4em",
        whiteSpace: "pre-wrap",
        //paddingBottom: 12,
        textRendering: "auto",
        textAlign: 'start',
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

    shoutContainer: {},
    shoutInfoContainer: {},
    shoutHeadshotContainer: {
        width: 48,
        height: 48,
        borderRadius: '50%',
        padding: 0,
        boxShadow: 'rgba(25, 25, 25, 0.3) 0px 1px 4px 0px',
        transition: 'box-shadow 200ms ease-in-out',
        overflow: 'hidden',
        backgroundColor: 'var(--text-color-secondary)',
        '&:hover': {
            boxShadow: 'rgba(25, 25, 25, 0.75) 0px 1px 6px 0px',
        },
    },
    shoutInfo: {
        marginLeft: 12,
        '& div': {
            marginTop: 12,
            width: '100%',
            color: 'var(--text-color-secondary)',
            lineHeight: '1.4em',
            fontSize: 12,
            fontWeight: 400,
            textAlign: 'start',
        },
    },
    shoutBody: {
        marginTop: 6,
        marginBottom: 0,
        color: "var(--text-color-primary)",
        fontSize: 16,
        wordWrap: "break-word",
        fontFamily: "HCo Gotham SSm,Helvetica Neue,Helvetica,Arial,Lucida Grande,sans-serif",
        fontWeight: 400,
        lineHeight: "1.4em",
        whiteSpace: "pre-wrap",
        textRendering: "auto",
        textAlign: 'start',
    },
    shoutPostContainer: {
        marginTop: 12,
        width: '100%',
    },
    shoutInputContainer: {
        marginBottom: 0,
        flexGrow: 1,
    },
    shoutInput: {
        width: '100%',
        height: 38,
        lineHeight: '1.6em',
        resize: 'none',
        borderRadius: 3,
        border: '1px solid var(--text-color-secondary)',
        padding: '5px 12px',
        fontSize: 16,
        fontWeight: 400,
        '-webkit-appearance': 'none',
        paddingRight: 30,
        '&:focus': {
            boxShadow: 'none',
            borderColor: 'var(--primary-color)',
            outline: 0,
        }
    },
    shoutRemainingChar: {
        '& span': {
            fontSize: 10,
            fontWeight: 400,
            color: 'var(--text-color-secondary)',
            lineHeight: '1.5em',
        },
    },
    shoutSubmitBtn: {
        fontSize: 18,
        fontWeight: 500,
        lineHeight: '100%',
        margin: '0 0 0 12px',
        padding: 9,
    },
});

// have: shout, description, games (should have nothing), members, social links
const AboutTab = ({}: {}) => {
    const s = useStyles();
    const buttonStyles = useButtonStyles();
    const store = GroupsPageStore.useContainer();
    const {group,userPerms, members} = GroupsPageStore.useContainer();

    const textAreaRef = useRef(null);
    const [textAreaRemainingChar, setTextAreaRemainingChar] = useState(0);

    return <div>
        {
            userPerms && userPerms.permissions.groupPostsPermissions.viewStatus && group.shout ? <Section header={"Shout"}>
                <div className={`${s.shoutContainer} section-content noShadow`}>
                    <div className={`${s.shoutInfoContainer} flex`}>
                        <NewLink className={`${s.shoutHeadshotContainer}`} href={'/users/' + group.shout.poster.userId + '/profile'}>
                            <PlayerHeadshot id={group.shout.poster.userId} name={group.shout.poster.username}/>
                        </NewLink>
                        <div className={`${s.shoutInfo} flex flex-column align-items-start`}>
                            <CreatorLink type={'User'} id={group.shout.poster.userId} name={group.shout.poster.displayName} />
                            <span className={s.shoutBody}>{group.shout.body}</span>
                            <div>{dayjs(group.shout.updated).format('MMM D, YYYY | h:mm A')}</div>
                        </div>
                    </div>
                    {userPerms && userPerms.permissions.groupPostsPermissions.postToStatus ? <div className={`${s.shoutPostContainer} flex align-items-start`}>
                        <div className={`${s.shoutInputContainer} flex flex-column align-items-end`}>
                            <input onChange={() => {
                                setTextAreaRemainingChar(textAreaRef?.current?.value?.length)
                            }} className={s.shoutInput} placeholder="Enter your shout!" maxLength={255} ref={textAreaRef} />
                            <div className={`${s.shoutRemainingChar}`}>
                                <span>{textAreaRemainingChar}/255</span>
                            </div>
                        </div>
                        <ActionButton
                            label='Group Shout'
                            buttonStyle={buttonStyles.newContinueButton}
                            className={s.shoutSubmitBtn}
                            onClick={() => {
                                console.log("shouting!")
                            }}
                        />
                    </div> : null}
                </div>
            </Section> : null
        }
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
                {members.members.map(m => (
                    <MemberItem key={m.userId} member={m} />
                ))}
            </ul>}
        </Section>
        {/*<Section header={"Social Links"} contentSectioned={true}>*/}
        {/*    <pre className={`${s.description} w-100 m-0 overflow-hidden `}>{store.group.description}</pre>*/}
        {/*</Section>*/}
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