import { createUseStyles } from 'react-jss';
import { useState, useEffect } from 'react';
import Dropdown2016, { DropdownOption } from "../../dropdown2016";

const useStyles = createUseStyles({

})

const GroupDropdown = ({}: {}) => {
    const s = useStyles();

    const DropdownOptions: DropdownOption[] = [
        {
            name: 'Report Abuse',
            url: '/internal/report-abuse',
        },
    ]

    return <Dropdown2016 options={DropdownOptions} />
};

export default GroupDropdown;