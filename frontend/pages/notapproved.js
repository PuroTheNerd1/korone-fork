import { useState, useEffect } from 'react';
import { useRouter } from 'next/router';
import { getMyBanData, unlockMyBan } from '../services/users';
import { logout } from '../services/auth';
import Head from 'next/head';

function formatDate(dateStr) {
  const d = new Date(dateStr);
  return d.toLocaleString('en-US', {
    month: 'numeric',
    day: 'numeric',
    year: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
    second: '2-digit',
    hour12: true,
  });
}

function getBanTitle(ban) {
  if (!ban.expiredAt) return 'Account Deleted';
  const remaining = new Date(ban.expiredAt) - new Date();
  const days = remaining / (1000 * 60 * 60 * 24);
  if (remaining / (1000 * 60) <= 1) return 'Warned';
  if (days <= 1) return 'Banned for 1 Day';
  if (days <= 3) return 'Banned for 3 Days';
  if (days <= 7) return 'Banned for 1 Week';
  if (days <= 14) return 'Banned for 2 Weeks';
  if (days <= 30) return 'Banned for 1 Month';
  if (days <= 365) return 'Banned for 1 Year';
  return `Banned for ${Math.floor(days)} Days`;
}

function getBanDurationText(ban) {
  if (!ban.expiredAt) return null;
  const total = new Date(ban.expiredAt) - new Date(ban.createdAt);
  const days = total / (1000 * 60 * 60 * 24);
  if (days <= 1) return '1 day';
  if (days <= 3) return '3 days';
  if (days <= 7) return '1 week';
  if (days <= 14) return '2 weeks';
  if (days <= 30) return '1 month';
  if (days <= 365) return '1 year';
  return `${Math.floor(days)} days`;
}

export default function NotApprovedPage() {
  const router = useRouter();
  const [ban, setBan] = useState(null);
  const [loading, setLoading] = useState(true);
  const [unlocking, setUnlocking] = useState(false);

  useEffect(() => {
    getMyBanData()
      .then(data => {
        setBan(data);
        setLoading(false);
      })
      .catch(() => {
        router.replace('/home');
      });
  }, []);

  const handleUnlock = async () => {
    setUnlocking(true);
    try {
      await unlockMyBan();
      router.replace('/home');
    } catch {
      setUnlocking(false);
    }
  };

  const handleLogout = async () => {
    try {
      await logout();
    } catch {
      // ignore errors
    }
    router.replace('/login');
  };

  if (loading) return null;
  if (!ban) return null;

  const isPerm = !ban.expiredAt;
  const title = getBanTitle(ban);
  const durationText = getBanDurationText(ban);

  const font = "'Source Sans Pro', sans-serif";
  const linkStyle = { color: '#004ab3' };
  const w400 = { fontWeight: 400 };

  return (
    <>
      <Head>
        <link href="https://fonts.googleapis.com/css2?family=Source+Sans+Pro:wght@300;400&display=swap" rel="stylesheet" />
      </Head>
      <div style={{ minHeight: '100vh', backgroundColor: '#e8e8e8', display: 'flex', alignItems: 'flex-start', justifyContent: 'center', paddingTop: '40px' }}>
        <div style={{ fontFamily: font, fontWeight: 300, fontSize: '14px', padding: '20px', width: '560px', backgroundColor: '#fff', border: '1px solid #c3c3c3', color: '#000' }}>
          <h1 style={{ fontFamily: font, fontSize: '22px', fontWeight: 400, marginBottom: '12px' }}>{title}</h1>

          {!isPerm ? (
            <p style={{ marginBottom: '8px' }}>
              Our content monitors have determined that your behavior at Korone has been in violation of our Terms of Service. We will terminate your account if you do not abide by the rules.
            </p>
          ) : (
            <p style={{ marginBottom: '8px' }}>
              Your account has been permanently banned from this site for the following reason.
            </p>
          )}

          <p style={{ ...w400, marginBottom: '8px' }}>Reviewed: {formatDate(ban.createdAt)}</p>

          <p style={{ ...w400, marginBottom: '8px' }}>
            <span style={{ fontWeight: 400 }}>Moderator Note:</span> {ban.reason}
          </p>

          {!isPerm && (
            <>
              <p style={{ marginBottom: '8px' }}>
                Please abide by the{' '}
                <a href="/auth/tos" style={linkStyle}>
                  Korone Community Guidelines
                </a>{' '}
                so that Korone can be fun for users of all ages.
              </p>
              {durationText && ban.expiredAt && (
                <p style={{ marginBottom: '8px' }}>
                  Your account has been disabled for {durationText}. You may re-activate it after {formatDate(ban.expiredAt)}.
                </p>
              )}
            </>
          )}

          {isPerm && (
            <p style={{ marginBottom: '8px' }}>
              If you attempt to create accounts to bypass a ban, we may ban your new accounts at any time.
            </p>
          )}

          <p style={{ marginBottom: '16px' }}>
            If you wish to appeal, please contact us via the{' '}
            <a href="https://discord.com/channels/1411352883533844603/1412917961647325224" style={linkStyle}>
              Support Form
            </a>
            .
          </p>

          {ban.canUnlock && (
            <div style={{ marginBottom: '12px' }}>
              <p style={{ marginBottom: '8px' }}>
                You can unlock your account by clicking the button below. Further violations of our rules may lead to account deletion.
              </p>
              <button
                onClick={handleUnlock}
                disabled={unlocking}
                style={{ fontFamily: font, fontSize: '11px', padding: '1px 10px', cursor: 'pointer', marginRight: '8px' }}
              >
                {unlocking ? 'Unlocking...' : 'Unlock Account'}
              </button>
            </div>
          )}

          <div style={{ textAlign: 'center' }}>
            <button
              onClick={handleLogout}
              style={{ fontFamily: font, fontSize: '11px', padding: '1px 10px', cursor: 'pointer' }}
            >
              Logout
            </button>
          </div>
        </div>
      </div>
    </>
  );
}

NotApprovedPage.getInitialProps = () => {
  return {
    title: 'Account Suspended - Korone',
    disableWebsiteTheming: true,
  };
};
