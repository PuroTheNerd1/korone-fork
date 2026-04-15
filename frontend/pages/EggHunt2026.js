import React, { useEffect } from 'react';
import Head from 'next/head';

const IMGS = {
    bg:            '/egghunt2026/bg.png',
    robloxLogo:    '/egghunt2026/korone-logo.png',
    eventLogo:     '/egghunt2026/event-logo.png',
    descriptionBg: '/egghunt2026/description-bg.png',
    game1Label:    '/egghunt2026/game1-label.png',
    game1:         '/egghunt2026/game1.png',
    trailerLabel:  '/egghunt2026/trailer-label.png',
    trailer:       'https://cdn.pekora.zip/KoroneEggHunt_1440p30fps_Final.mp4',
};

const EggHunt2026 = () => {
    useEffect(() => {
        const el = document.querySelector('[class^="chatMenu_"]');
        if (el) el.style.display = 'none';
    }, []);

    return (
        <>
            <Head>
                <title>Korone - Egg Hunt 2026</title>
                <link
                    href="https://fonts.googleapis.com/css2?family=Source+Sans+Pro:wght@300;400;600;700&display=swap"
                    rel="stylesheet"
                />
                <link rel="stylesheet" href="/css/egghunt-override.css" />
            </Head>

            <div
                id="EggHuntPage"
                style={{
                    marginTop: -12,
                    width: '100vw',
                    minWidth: 1185,
                    minHeight: '100vh',
                    position: 'relative',
                    backgroundImage: `url(${IMGS.bg})`,
                    backgroundSize: '1440px auto',
                    backgroundPosition: 'center top',
                    backgroundRepeat: 'repeat',
                    backgroundAttachment: 'scroll',
                    fontFamily: "'Source Sans Pro', Arial, Helvetica, sans-serif",
                    fontSize: 14,
                    color: '#343434',
                    paddingBottom: 80,
                }}
            >
                <div
                    style={{
                        position: 'fixed',
                        top: 0,
                        left: 0,
                        width: '100%',
                        height: 66,
                        backgroundColor: '#1a1a1a',
                        zIndex: 1000,
                        display: 'flex',
                        alignItems: 'center',
                        color: '#fff',
                    }}
                >
                    <div
                        style={{
                            display: 'flex',
                            alignItems: 'center',
                            padding: '12px',
                            gap: 8,
                        }}
                    >
                        <a
                            href="/home"
                            style={{
                                display: 'inline-flex',
                                alignItems: 'center',
                                gap: 4,
                                color: '#aaa',
                                textDecoration: 'none',
                                fontSize: 13,
                                marginRight: 8,
                                paddingRight: 12,
                                borderRight: '1px solid #444',
                            }}
                        >
                            &#8592; Go Back
                        </a>
                        <a href="/" style={{ display: 'inline-block', marginTop: 3 }}>
                            <img
                                src={IMGS.robloxLogo}
                                alt="ROBLOX"
                                style={{ width: 36, height: 36, display: 'block' }}
                            />
                        </a>
                        <nav style={{ display: 'inline-block', paddingTop: 5 }}>
                            <ul
                                style={{
                                    listStyle: 'none',
                                    margin: 0,
                                    padding: 0,
                                    display: 'flex',
                                    position: 'relative',
                                }}
                            >
                                <li>
                                    <a
                                        href="#"
                                        onClick={(e) => {
                                            e.preventDefault();
                                            window.scrollTo({ top: 0, behavior: 'smooth' });
                                        }}
                                        style={{
                                            color: '#fff',
                                            textDecoration: 'none',
                                            fontSize: 14,
                                            fontWeight: 600,
                                        }}
                                    >
                                        Egg Hunt 2026
                                    </a>
                                </li>
                                <li
                                    style={{
                                        position: 'absolute',
                                        bottom: -5,
                                        left: 12.5,
                                        width: 57,
                                        height: 2,
                                        backgroundColor: '#fff',
                                        listStyle: 'none',
                                    }}
                                />
                            </ul>
                        </nav>
                    </div>
                </div>

                <div style={{ maxWidth: 1000, margin: '0 auto', paddingTop: 86 }}>
                    <div style={{ textAlign: 'center', padding: '24px 0' }}>
                        <img
                            src={IMGS.eventLogo}
                            alt="Egg Hunt 2026: Fractured Ideals"
                            style={{ width: 480, height: 'auto' }}
                        />
                    </div>

                    <div style={{ position: 'relative', margin: '12px 0 20px' }}>
                        <img
                            src={IMGS.descriptionBg}
                            alt=""
                            style={{ width: '100%', height: 'auto', display: 'block' }}
                        />
                        <p style={{
                            position: 'absolute',
                            top: '50%',
                            left: '10%',
                            width: '80%',
                            transform: 'translateY(-50%)',
                            margin: 0,
                            fontSize: 20,
                            color: '#000',
                            lineHeight: 1.5,
                        }}>
                            Eggs are appearing across the Eggverse, scattered through strange lands,
                            forgotten places, and hidden corners waiting to be discovered. Some are
                            easy to find... others will truly test your skills. Team up with your
                            friends and explore a variety of unique worlds as you hunt down over 40
                            eggs! Do you have what it takes to collect them all?
                        </p>
                    </div>

                    <img
                        src={IMGS.game1Label}
                        alt="Egg Hunt 2026: Fractured Ideals"
                        style={{ width: '100%', height: 'auto', display: 'block', margin: '0 0 8px' }}
                    />

                    <a
                        href="https://www.pekora.zip/games/495942/Egg-Hunt-2026-Fractured-Ideals"
                        style={{ textDecoration: 'none', display: 'block', margin: '0 0 20px', position: 'relative' }}
                    >
                        <img
                            src={IMGS.game1}
                            alt="Egg Hunt 2026: Fractured Ideals"
                            style={{ width: '100%', height: 'auto', display: 'block' }}
                        />
                        <p
                            style={{
                                position: 'absolute',
                                top: '18%',
                                left: '50%',
                                width: '44%',
                                margin: 0,
                                fontSize: 19,
                                color: '#0055b3',
                                lineHeight: 1.5,
                            }}
                        >
                            Set out on an eggciting journey across diverse worlds, from peaceful
                            villages to dangerous volcanic lands. Over 40 eggs lie hidden throughout
                            the Eggverse, each with its own challenge to overcome.
                        </p>
                    </a>

                    <img
                        src={IMGS.trailerLabel}
                        alt="Trailer"
                        style={{ width: '100%', height: 'auto', display: 'block', margin: '0 0 8px' }}
                    />

                    <div style={{ textAlign: 'center', margin: '0 0 20px' }}>
                        <video
                            controls
                            style={{
                                width: 700,
                                borderRadius: 4,
                                display: 'block',
                                margin: '0 auto',
                            }}
                        >
                            <source src={IMGS.trailer} type="video/mp4" />
                        </video>
                    </div>
                </div>
            </div>
        </>
    );
};

EggHunt2026.getInitialProps = () => {
    return {
        title: 'Korone - Egg Hunt 2026',
        disableWebsiteTheming: true,
    };
};

export default EggHunt2026;
