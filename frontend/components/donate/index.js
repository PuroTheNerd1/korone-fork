import React, { useEffect, useState } from "react";
import { createUseStyles } from "react-jss";
import { getTheme, themeType } from "../../services/theme";

const BASE_URL = 'https://www.pekora.zip';

const tiers = [
    { amount: 5, assetId: 518724 },
    { amount: 25, assetId: 518780 },
    { amount: 50, assetId: 518806 },
    { amount: 100, assetId: 518744 },
];

const paymentMethods = [
    {
        name: 'Stripe',
        value: null,
        url: '/donate/stripe',
        note: null,
    },
    {
        name: 'PayPal',
        value: '@koronezip',
        url: 'https://paypal.me/koronezip',
        note: 'Friends & Family Only',
    },
];

const cryptoMethods = [
    {
        name: 'Ethereum',
        ticker: 'ETH',
        address: '0xE4283B453182E4514b1e7Cf1de12d71cc726b3F8',
    },
    {
        name: 'Bitcoin',
        ticker: 'BTC',
        address: 'bc1qpwkvukjucw4j488d228pc3yhephfjp9l47q5ju',
    },
    {
        name: 'Litecoin',
        ticker: 'LTC',
        address: 'LgkgqhFd1zLHAe7kymV472RssjXWy1tYgg',
    },
    {
        name: 'Solana',
        ticker: 'SOL',
        address: '1EjmdfgcjNgD7sNv4mQ6SKvX51iBw2z8wJLpDgPmf9P',
    },
];

const useStyles = createUseStyles({
    wrapper: {
        paddingTop: '20px',
        paddingBottom: '40px',
    },
    hero: {
        textAlign: 'center',
        marginBottom: '24px',
    },
    title: {
        color: 'var(--text-color-primary)',
        fontSize: '32px',
        fontWeight: 700,
        marginBottom: '8px',
    },
    subTitle: {
        color: 'var(--text-color-secondary)',
        fontSize: '15px',
        maxWidth: '640px',
        margin: '0 auto',
        lineHeight: 1.5,
    },
    grid: {
        display: 'grid',
        gridTemplateColumns: 'repeat(4, 1fr)',
        gap: '18px',
        '@media(max-width: 991px)': {
            gridTemplateColumns: 'repeat(2, 1fr)',
        },
        '@media(max-width: 500px)': {
            gridTemplateColumns: '1fr',
        },
    },
    card: {
        background: p => p.theme === themeType.dark || p.theme === themeType.obc2019 ? '#393939' : 'var(--white-color)',
        borderRadius: '6px',
        boxShadow: '0 1px 3px rgba(25, 25, 25, 0.15)',
        padding: '16px',
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        transition: 'transform 150ms ease, box-shadow 150ms ease',
        textDecoration: 'none',
        color: 'inherit',
        '&:hover': {
            transform: 'translateY(-3px)',
            boxShadow: '0 4px 10px rgba(25, 25, 25, 0.25)',
            textDecoration: 'none',
            color: 'inherit',
        },
    },
    thumbWrap: {
        width: '100%',
        aspectRatio: '1 / 1',
        background: p => p.theme === themeType.dark || p.theme === themeType.obc2019 ? '#2a2a2a' : 'var(--background-color)',
        borderRadius: '4px',
        overflow: 'hidden',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        marginBottom: '12px',
        perspective: '800px',
        position: 'relative',
    },
    '@keyframes donateSpin3d': {
        '0%': { transform: 'rotateY(0deg)' },
        '100%': { transform: 'rotateY(360deg)' },
    },
    thumb: {
        width: '100%',
        height: '100%',
        objectFit: 'contain',
        transformStyle: 'preserve-3d',
        animation: '$donateSpin3d 4s linear infinite',
    },
    badgeContainer: {
        position: 'absolute',
        top: 8,
        right: 8,
        display: 'flex',
        gap: 4,
        zIndex: 2,
    },
    badge: {
        display: 'inline-flex',
        alignItems: 'center',
        backgroundColor: '#F23515',
        color: '#fff',
        padding: '4px 6px',
        borderRadius: 3,
        fontWeight: 500,
        fontSize: 10,
        lineHeight: '1em',
        boxShadow: '0 1px 2px rgba(0, 0, 0, 0.2)',
    },
    badgeClockIcon: {
        backgroundPosition: '-16px -576px!important',
        width: 16,
        height: 16,
        backgroundSize: '32px auto',
    },
    amount: {
        color: 'var(--primary-color)',
        fontSize: '26px',
        fontWeight: 700,
        margin: 0,
        marginBottom: '2px',
    },
    cadence: {
        color: 'var(--text-color-secondary)',
        fontSize: '13px',
        margin: 0,
        marginBottom: '10px',
    },
    ctaRow: {
        width: '100%',
        textAlign: 'center',
        background: 'var(--primary-color)',
        color: 'white',
        fontWeight: 500,
        fontSize: '14px',
        padding: '7px 0',
        borderRadius: '4px',
    },
    section: {
        marginTop: '44px',
    },
    sectionTitle: {
        color: 'var(--text-color-primary)',
        fontSize: '22px',
        fontWeight: 700,
        marginBottom: '12px',
        borderBottom: '1px solid var(--text-color-secondary)',
        paddingBottom: '6px',
    },
    perksBox: {
        background: p => p.theme === themeType.dark || p.theme === themeType.obc2019 ? '#393939' : 'var(--white-color)',
        borderRadius: '6px',
        boxShadow: '0 1px 3px rgba(25, 25, 25, 0.15)',
        padding: '18px 22px',
        color: 'var(--text-color-primary)',
    },
    perkItem: {
        display: 'flex',
        alignItems: 'flex-start',
        marginBottom: '10px',
        '&:last-child': { marginBottom: 0 },
    },
    perkBadge: {
        display: 'inline-block',
        background: 'var(--primary-color)',
        color: 'white',
        fontSize: '12px',
        fontWeight: 600,
        padding: '3px 10px',
        borderRadius: '4px',
        marginRight: '10px',
        whiteSpace: 'nowrap',
        textTransform: 'uppercase',
        letterSpacing: '0.5px',
    },
    perkText: {
        margin: 0,
        lineHeight: 1.5,
        fontSize: '14px',
    },
    paymentGrid: {
        display: 'grid',
        gridTemplateColumns: 'repeat(3, 1fr)',
        gap: '14px',
        '@media(max-width: 767px)': {
            gridTemplateColumns: '1fr',
        },
    },
    paymentCard: {
        background: p => p.theme === themeType.dark || p.theme === themeType.obc2019 ? '#393939' : 'var(--white-color)',
        borderRadius: '6px',
        boxShadow: '0 1px 3px rgba(25, 25, 25, 0.15)',
        padding: '16px',
        display: 'flex',
        flexDirection: 'column',
    },
    paymentName: {
        fontSize: '16px',
        fontWeight: 700,
        color: 'var(--text-color-primary)',
        margin: 0,
        marginBottom: '4px',
    },
    paymentLink: {
        color: 'var(--primary-color)',
        fontSize: '15px',
        fontWeight: 500,
        wordBreak: 'break-all',
        '&:hover': {
            color: 'var(--primary-color-hover)',
            textDecoration: 'underline',
        },
    },
    paymentNote: {
        color: 'var(--text-color-secondary)',
        fontSize: '12px',
        marginTop: '6px',
        marginBottom: 0,
        fontStyle: 'italic',
        lineHeight: 1.4,
    },
    cryptoGrid: {
        display: 'grid',
        gridTemplateColumns: 'repeat(2, 1fr)',
        gap: '14px',
        '@media(max-width: 767px)': {
            gridTemplateColumns: '1fr',
        },
    },
    cryptoCard: {
        background: p => p.theme === themeType.dark || p.theme === themeType.obc2019 ? '#393939' : 'var(--white-color)',
        borderRadius: '6px',
        boxShadow: '0 1px 3px rgba(25, 25, 25, 0.15)',
        padding: '16px',
        display: 'flex',
        flexDirection: 'column',
    },
    cryptoName: {
        fontSize: '16px',
        fontWeight: 700,
        color: 'var(--text-color-primary)',
        margin: 0,
        marginBottom: '8px',
    },
    cryptoTicker: {
        color: 'var(--text-color-secondary)',
        fontWeight: 400,
        fontSize: '13px',
        marginLeft: '6px',
    },
    cryptoAddress: {
        fontFamily: 'monospace',
        fontSize: '13px',
        background: p => p.theme === themeType.dark || p.theme === themeType.obc2019 ? '#2a2a2a' : '#f4f4f4',
        color: p => p.theme === themeType.dark || p.theme === themeType.obc2019 ? '#e0e0e0' : '#222',
        borderRadius: '4px',
        padding: '8px 12px',
        wordBreak: 'break-all',
        cursor: 'pointer',
        border: '1px solid transparent',
        transition: 'border-color 150ms ease, background 150ms ease',
        userSelect: 'none',
        '&:hover': {
            borderColor: 'var(--primary-color)',
        },
    },
    cryptoAddressCopied: {
        fontFamily: 'monospace',
        fontSize: '13px',
        background: 'var(--primary-color)',
        color: 'white',
        borderRadius: '4px',
        padding: '8px 12px',
        wordBreak: 'break-all',
        cursor: 'pointer',
        border: '1px solid transparent',
        transition: 'border-color 150ms ease, background 150ms ease',
        userSelect: 'none',
        textAlign: 'center',
        fontStyle: 'italic',
    },
    disclaimer: {
        textAlign: 'center',
        marginTop: '28px',
        color: 'var(--text-color-secondary)',
        fontSize: '13px',
    },
    countdownBanner: {
        background: '#F23515',
        borderRadius: '8px',
        padding: '18px 22px',
        marginBottom: '24px',
        color: 'white',
        boxShadow: '0 4px 14px rgba(242, 53, 21, 0.35)',
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        textAlign: 'center',
    },
    countdownLabel: {
        fontSize: '13px',
        fontWeight: 600,
        textTransform: 'uppercase',
        letterSpacing: '1.5px',
        margin: 0,
        marginBottom: '4px',
        opacity: 0.95,
    },
    countdownTitle: {
        fontSize: '20px',
        fontWeight: 700,
        margin: 0,
        marginBottom: '12px',
    },
    countdownGrid: {
        display: 'flex',
        gap: '10px',
        flexWrap: 'wrap',
        justifyContent: 'center',
    },
    countdownUnit: {
        background: 'rgba(0, 0, 0, 0.25)',
        borderRadius: '6px',
        padding: '8px 14px',
        minWidth: '68px',
    },
    countdownValue: {
        fontSize: '26px',
        fontWeight: 700,
        margin: 0,
        lineHeight: 1,
        fontVariantNumeric: 'tabular-nums',
    },
    countdownUnitLabel: {
        fontSize: '11px',
        textTransform: 'uppercase',
        letterSpacing: '1px',
        margin: 0,
        marginTop: '4px',
        opacity: 0.9,
    },
});

const getTimeUntilNextMonth = () => {
    const now = new Date();
    const next = new Date(now.getFullYear(), now.getMonth() + 1, 1, 0, 0, 0, 0);
    let diff = Math.max(0, next.getTime() - now.getTime());
    const days = Math.floor(diff / 86400000); diff -= days * 86400000;
    const hours = Math.floor(diff / 3600000); diff -= hours * 3600000;
    const minutes = Math.floor(diff / 60000); diff -= minutes * 60000;
    const seconds = Math.floor(diff / 1000);
    return { days, hours, minutes, seconds };
};

const CryptoAddress = ({ address, styles }) => {
    const [copied, setCopied] = useState(false);

    const handleClick = () => {
        navigator.clipboard.writeText(address).then(() => {
            setCopied(true);
            setTimeout(() => setCopied(false), 1000);
        });
    };

    return (
        <div
            className={copied ? styles.cryptoAddressCopied : styles.cryptoAddress}
            onClick={handleClick}
            title="Click to copy"
        >
            {copied ? 'Copied to clipboard!' : address}
        </div>
    );
};

const Donate = () => {
    const [thumbs, setThumbs] = useState({});
    const [countdown, setCountdown] = useState(getTimeUntilNextMonth);
    const s = useStyles({ theme: getTheme() });

    useEffect(() => {
        const id = setInterval(() => setCountdown(getTimeUntilNextMonth()), 1000);
        return () => clearInterval(id);
    }, []);

    useEffect(() => {
        const ids = tiers.map(t => t.assetId).join(',');
        fetch(`${BASE_URL}/apisite/thumbnails/v1/assets?assetIds=${ids}`)
            .then(r => r.json())
            .then(json => {
                const map = {};
                (json.data || []).forEach(entry => {
                    if (entry.state === 'Completed' && entry.imageUrl) {
                        map[entry.targetId] = entry.imageUrl;
                    }
                });
                setThumbs(map);
            })
            .catch(() => {});
    }, []);

    return <div className={`container ${s.wrapper}`}>
        <div className={s.hero}>
            <h1 className={s.title}>Support Korone</h1>
            <p className={s.subTitle}>
                Korone is a non-profit project run by the community, for the community. Every dollar goes straight
                into better hosting and infrastructure &mdash; nothing else. For full legal transparency, we publish
                a live breakdown of every cent we receive and spend in a dedicated channel on our Discord server.
            </p>
            <p className={s.subTitle} style={{ marginTop: '10px' }}>
                Pick a tier below to donate and grab the matching item as a thank-you.
            </p>
        </div>

        <div className={s.countdownBanner}>
            <p className={s.countdownLabel}>Leaving Soon</p>
            <p className={s.countdownTitle}>These donation items disappear at the end of the month</p>
            <div className={s.countdownGrid}>
                <div className={s.countdownUnit}>
                    <p className={s.countdownValue}>{String(countdown.days).padStart(2, '0')}</p>
                    <p className={s.countdownUnitLabel}>Days</p>
                </div>
                <div className={s.countdownUnit}>
                    <p className={s.countdownValue}>{String(countdown.hours).padStart(2, '00')}</p>
                    <p className={s.countdownUnitLabel}>Hours</p>
                </div>
                <div className={s.countdownUnit}>
                    <p className={s.countdownValue}>{String(countdown.minutes).padStart(2, '0')}</p>
                    <p className={s.countdownUnitLabel}>Minutes</p>
                </div>
                <div className={s.countdownUnit}>
                    <p className={s.countdownValue}>{String(countdown.seconds).padStart(2, '0')}</p>
                    <p className={s.countdownUnitLabel}>Seconds</p>
                </div>
            </div>
        </div>

        <div className={s.grid}>
            {tiers.map(tier => {
                const thumb = thumbs[tier.assetId] || '/img/placeholder.png';
                const itemUrl = `${BASE_URL}/catalog/${tier.assetId}/--`;
                return <a
                    key={tier.assetId}
                    href={itemUrl}
                    className={s.card}
                >
                    <div className={s.thumbWrap}>
                        <div className={s.badgeContainer}>
                            <span className={s.badge}>New</span>
                            <span className={s.badge}>
                                <span className={`${s.badgeClockIcon} icon-clock`}/>
                            </span>
                        </div>
                        <img src={thumb} alt={`$${tier.amount} donation item`} className={s.thumb}/>
                    </div>
                    <p className={s.amount}>${tier.amount}</p>
                    <div className={s.ctaRow}>Donate ${tier.amount}</div>
                </a>;
            })}
        </div>

        <div className={s.section}>
            <h2 className={s.sectionTitle}>Perks</h2>
            <div className={s.perksBox}>
                <div className={s.perkItem}>
                    <span className={s.perkBadge}>Item</span>
                    <p className={s.perkText}>Every tier grants the matching in-game item shown above.</p>
                </div>
                <div className={s.perkItem}>
                    <span className={s.perkBadge}>Discord</span>
                    <p className={s.perkText}>
                        Get the <strong>Donator</strong> role in the Korone Discord server permanently.
                    </p>
                </div>
            </div>
        </div>

        <div className={s.section}>
            <h2 className={s.sectionTitle}>Payment Methods</h2>
            <div className={s.paymentGrid}>
                {paymentMethods.map(method => (
                    <div key={method.name} className={s.paymentCard}>
                        {method.value === null ? (
                            <a
                                href={method.url}
                                target='_blank'
                                rel='noopener noreferrer'
                                className={s.paymentName}
                                style={{ textDecoration: 'none', cursor: 'pointer' }}
                            >
                                {method.name}
                            </a>
                        ) : (
                            <>
                                <p className={s.paymentName}>{method.name}</p>
                                <a
                                    href={method.url}
                                    target='_blank'
                                    rel='noopener noreferrer'
                                    className={s.paymentLink}
                                >
                                    {method.value}
                                </a>
                            </>
                        )}
                        {method.note && <p className={s.paymentNote}>{method.note}</p>}
                    </div>
                ))}
            </div>
        </div>

        <div className={s.section}>
            <h2 className={s.sectionTitle}>Cryptocurrency</h2>
            <div className={s.cryptoGrid}>
                {cryptoMethods.map(coin => (
                    <div key={coin.ticker} className={s.cryptoCard}>
                        <p className={s.cryptoName}>
                            {coin.name}
                            <span className={s.cryptoTicker}>{coin.ticker}</span>
                        </p>
                        <CryptoAddress address={coin.address} styles={s} />
                    </div>
                ))}
            </div>
        </div>

        <p className={s.disclaimer}>
            Donations are non-refundable. After donating, open a support ticket in our Discord with your receipt to claim your role and item.
        </p>
    </div>;
};

export default Donate;
