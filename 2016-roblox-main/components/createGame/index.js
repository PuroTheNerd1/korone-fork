import React, {useState} from "react";
import { createUseStyles } from "react-jss";
import OldVerticalTabs from "../oldVerticalTabs2";
import AuthenticationStore from "../../stores/authentication";
import Templates from "./subpages/templates";
import BasicSettings from "./subpages/basicSettings";
import Access from "./subpages/access";
import AdvancedSettings from "./subpages/advancedSettings";

const options = [
    {
        name: 'Templates',
        displayName: 'Templates',
        element: <Templates />,
    },
    {
        name: 'BasicSettings',
        displayName: 'Basic Settings',
        element: <BasicSettings />,
    },
    {
        name: 'Access',
        displayName: 'Access',
        element: <Access />,
    },
    {
        name: 'Advanced Settings',
        displayName: 'Advanced Settings',
        element: <AdvancedSettings />,
    }
]

const useStyles = createUseStyles({
    contentContainer: {
        padding: '15px',
    },
})

const CreateGame = props => {
    const s = useStyles();
    const auth = AuthenticationStore.useContainer();
    const [tab, setTab] = useState('Templates');
    
    if (!auth.userId) return null;
    
    return <div className='container'>
        <h1 style={{ fontWeight: 700, marginBottom: '10px' }}>Create Game</h1>
        <div>
            <OldVerticalTabs contentStyles={s.contentContainer} options={options} default={tab} onChange={n => setTab(n.name)}/>
        </div>
    </div>
}

export default CreateGame;