import React, { PropsWithChildren, useEffect, useState } from "react";

interface CountdownProps extends PropsWithChildren {
    timestamp: string;
    className: string;
};

function Countdown({ timestamp, className }: CountdownProps) {
    const [timeStr, setTimeStr] = useState(getTimeStr());
    
    useEffect(() => {
        const interval = setInterval(() => {
            setTimeStr(getTimeStr());
        }, 1000);
        return () => clearInterval(interval);
    }, [timestamp]);
    
    function getTimeStr() {
        const now = new Date();
        const timestampDate = new Date(timestamp);
        const diff = Math.max(0, timestampDate.valueOf() - now.valueOf());
        
        const hours = Math.floor(diff / (1000 * 60 * 60));
        const minutes = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60));
        const seconds = Math.floor((diff % (1000 * 60)) / 1000);
        
        return { hours, minutes, seconds };
    }
    
    return <span className={className}>
        <span>{timeStr.hours}</span>
        h
        <span>{timeStr.minutes}</span>
        m
        <span>{timeStr.seconds}</span>
        s
    </span>
}

export default Countdown;
