import { createUseStyles } from 'react-jss';
import { useState, useEffect } from 'react';
import Section from "./Section";
import NewLink from "../../NewLink";

const useStyles = createUseStyles({

})

const StoreTab = ({}: {}) => {
    const s = useStyles();

    return <div>
        <Section header="Store" contentSectioned={false} headerChildren={<>
            <NewLink href={""} />
        </>}>

        </Section>
    </div>
};

export default StoreTab;