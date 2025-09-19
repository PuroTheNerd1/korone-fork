import {createUseStyles} from "react-jss";
import { getTheme } from "../../services/theme";
import { useEffect, useState } from "react";

const useStyles = createUseStyles({
    container: {},
    wrapper: {
        width: '100%',
        height: 400,
    },
    header: {},
    subHeader: {},
});

function DownloadPage() {
    const s = useStyles({theme: getTheme()});
    const [platform, setPlatform] = useState(DownloadPlatforms.WinNT61Above);
    
    useEffect(() => {
        if (typeof navigator === 'undefined') {
            return;
        }
        const ua = navigator.userAgent.toLowerCase();
        if (ua.includes("windows nt")) {
            setPlatform(DownloadPlatforms.WinNT61Above);
        } else if (ua.includes("linux") && ua.includes("x86")) {
            setPlatform(DownloadPlatforms.Linux);
        } else if (ua.includes("android")) {
            setPlatform(DownloadPlatforms.Android);
        } else if (ua.includes("iphone")) {
            setPlatform(DownloadPlatforms.IOS);
        }
    }, []);
    
    return <div className={`container ${s.container}`}>
        <div className={`${s.wrapper} section-content`}>
            <h1 className={s.header}>Get Korone</h1>
            <h3 className={s.subHeader}>Join the fun and play Korone today! Download and install the Korone app for your device.</h3>
        </div>
        <div className={`${s.wrapper} section-content`}>
            <h1 className={s.header}>Frequently Asked Questions</h1>
            <h3 className={s.subHeader}>Below are some frequently asked questions by users during or before the installation process. If none of these help you, you may make a support ticket or ask for help in the Discord.</h3>
        </div>
    </div>
}

export const DownloadPlatforms = Object.freeze({
    WinNT61Above: 0,
    Linux: 1,
    Android: 2,
    IOS: 3,
});

export default DownloadPage;
