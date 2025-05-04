import {createUseStyles} from "react-jss";
import useButtonStyles from "../../../styles/buttonStyles";
import AuthenticationStore from "../../../stores/authentication";
import FeedbackStore from "../../../stores/feedback";
import Link from "../../link";

const useStyles = createUseStyles({
    avatarHeader: {
        marginBottom: 10
    }
});

const useAvCardStyles = createUseStyles({
    avatarCardContainer: {
        borderRadius: 3,
        width: "123px",
        display: "flex",
        flexDirection: "column",
    },
    avatarCardImage: {
        width: "100%",
        height: "100px",
        aspectRatio: "1 / 1",
        "& img": {
            width: "100%",
            height: "100%",
        }
    },
});

function AvatarCard() {
    const s = useAvCardStyles();
    
    return <div className={`section-content ${s.avatarCardContainer}`}>
        <div className={s.avatarCardImage}>
            <img src="https://goober.top/images/thumbnails/c28f676a9e4fd9b7891ccce63ba37b4d1f5740229775edb35e44f1808d292a60.png" />
        </div>
        <Link href={"/catalog/53/--"}>
            <a href={"/catalog/53/--"}>
                <span>Dominus Empyreus</span>
            </a>
        </Link>
    </div>
}

function AvatarCardList() {
    const s = useStyles();
    const buttonStyles = useButtonStyles();
    const auth = AuthenticationStore.useContainer();
    const feedback = FeedbackStore.useContainer();
    
    return <div className={`flex`}>
        <AvatarCard />
        <AvatarCard />
        <AvatarCard />
        <AvatarCard />
        <AvatarCard />
    </div>
}

export default AvatarCardList;
