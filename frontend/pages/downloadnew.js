import Download from "../components/download2"
import getFlag from "../lib/getFlag";
import Theme2016 from "../components/theme2016";

const DownloadPage = () => {
    if (!getFlag('downloadPageEnabled', false)) return null;
    return <Theme2016>
        <Download></Download>
    </Theme2016>
}

export default DownloadPage;