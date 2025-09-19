import AdBanner from "../../components/ad/adBanner";
import React from "react";
import AvatarEditor from "../../components/AvatarEditorPage";
import AvatarInfoStore from "../../components/AvatarEditorPage/stores/avatarInfoStore";
import Theme2016 from "../../components/theme2016";
import AvatarPageStore from "../../components/AvatarEditorPage/stores/avatarPageStore";
import Head from "next/head";
import Script from "next/script";
import { createUseStyles } from "react-jss";
import { getTheme, themeType } from "../../services/theme";

const useStyles = createUseStyles({
    avPageWrapper: {
        background: p => p.theme === themeType.obc2019 ? 'var(--background-color)' : 'transparent',
    },
});

const AvatarPage = () => {
    const s = useStyles({theme: getTheme()});
    return <Theme2016>
        <Head>
            <title>Avatar - Korone</title>
            <script src="/js/3d/three-r137/three.js" />
            <script src="/js/3d/three-r137/MTLLoaderr.js" />
            <script src="/js/3d/three-r137/OBJLoaderr.js" />
            <script src="/js/3d/three-r137/RobloxOrbitControls.js" />
            <script src="/js/3d/tween.js" />
        </Head>
        <div className={`${s.avPageWrapper} container flex flex-column ssp`}>
            <AdBanner context="MyCharacterPage"/>
            <AvatarInfoStore.Provider>
                <AvatarPageStore.Provider>
                    <AvatarEditor />
                </AvatarPageStore.Provider>
            </AvatarInfoStore.Provider>
        </div>
    </Theme2016>
}

export default AvatarPage;