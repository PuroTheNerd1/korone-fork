import { createUseStyles } from "react-jss";
import {useEffect, useState} from "react";
import {getGameTemplates} from "../../../../services/games";

const useStyles = createUseStyles({})

const Templates = props => {
    const s = useStyles();
    // 0 = loading, 1 = failed, array = suiccess
    const [templates, setTemplates] = useState(0);
    
    useEffect(() => {
        if (typeof templates === 'number') {
            getGameTemplates().then((data) => {})
        }
    }, []);
    
    return <div className='container'>
        <span>GAME TEMPLATES</span>
    </div>
}

export default Templates;