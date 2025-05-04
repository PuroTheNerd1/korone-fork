import AdBanner from "../../components/ad/adBanner";
import React from "react";
import AvatarEditor from "../../components/AvatarEditorPage";
import AvatarInfoStore from "../../components/AvatarEditorPage/stores/avatarInfoStore";
import Theme2016 from "../../components/theme2016";

const AvatarPage = () => {
    return <Theme2016>
        <div className="container flex flex-column" style={{gap: 10}}>
            <AdBanner context="MyCharacterPage"/>
            <AvatarInfoStore.Provider>
                <AvatarEditor />
            </AvatarInfoStore.Provider>
        </div>
    </Theme2016>
}

export default AvatarPage;