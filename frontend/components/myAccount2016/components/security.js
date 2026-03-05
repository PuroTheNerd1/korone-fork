import { useEffect, useState } from "react";
import useCardStyles from "../../userProfile/styles/card";
import useFormStyles from "../styles/forms";
import Subtitle from "./subtitle";
import { logoutFromAllOtherSessions, getSessions, revokeSession } from "../../../services/auth";
import { getTwoFactorInfo } from "../../../services/accountSettings";
import MyAccountStore from "../stores/myAccountStore";

const formatLocalDateTime = (isoString) => {
  if (!isoString) return 'Unknown';
  const date = new Date(isoString);
  return date.toLocaleString(undefined, {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
};

const refreshSessions = (setSessions) => {
  getSessions().then(data => {
    setSessions(data && data.data ? data.data : data);
  });
};

const Security = props => {
  const cardStyles = useCardStyles();
  const s = useFormStyles();
  const store = MyAccountStore.useContainer();
  const [twoFactorInfo, setTwoFactorInfo] = useState(null);
  const [sessions, setSessions] = useState(null);
  const [sessionsError, setSessionsError] = useState(false);
  const [sessionsExpanded, setSessionsExpanded] = useState(false);

  useEffect(() => {
    getTwoFactorInfo().then(setTwoFactorInfo);
    getSessions().then(data => {
      setSessions(data && data.data ? data.data : data);
    }).catch(() => setSessionsError(true));
  }, []);

  return <div className='row'>
    <div className='col-12 mt-2'>
      <Subtitle>Active Sessions</Subtitle>
      <div className={cardStyles.card}>
        <button
          onClick={() => setSessionsExpanded(v => !v)}
          style={{
            width: '100%',
            background: 'none',
            border: 'none',
            padding: '12px 16px',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            cursor: 'pointer',
            fontSize: '14px',
            color: 'var(--text-color-primary)',
          }}
        >
          <span>
            {sessions === null && !sessionsError ? 'Loading...' : sessionsError ? 'Failed to load' : `${sessions.length} session${sessions.length !== 1 ? 's' : ''}`}
          </span>
          <span style={{ fontSize: '12px', color: 'var(--text-color-secondary)' }}>
            {sessionsExpanded ? '▲ Collapse' : '▼ Expand'}
          </span>
        </button>
        {sessionsExpanded && (
          <div style={{ borderTop: '1px solid var(--text-color-quinary)', padding: '12px 16px' }}>
            {sessionsError ? (
              <span className='text-muted'>Failed to load sessions.</span>
            ) : sessions === null ? (
              <span className='text-muted'>Loading...</span>
            ) : sessions.length === 0 ? (
              <span className='text-muted'>No active sessions found.</span>
            ) : (
              <div className='table-responsive'>
                <table className='table table-borderless mb-0' style={{ fontSize: '14px' }}>
                  <thead>
                    <tr style={{ borderBottom: '1px solid var(--text-color-quinary)' }}>
                      <th style={{ fontWeight: 600, paddingLeft: 0 }}>Platform</th>
                      <th style={{ fontWeight: 600 }}>Browser</th>
                      <th style={{ fontWeight: 600 }}>Logged in</th>
                      <th style={{ fontWeight: 600 }}>Last seen</th>
                      <th></th>
                    </tr>
                  </thead>
                  <tbody>
                    {sessions.map((session, idx) => (
                      <tr key={idx} style={session.isCurrent ? { background: 'var(--background-color-secondary, rgba(0,0,0,0.04))' } : {}}>
                        <td style={{ paddingLeft: 0 }}>{session.platform}</td>
                        <td>{session.browser}</td>
                        <td>{formatLocalDateTime(session.createdAt)}</td>
                        <td>{formatLocalDateTime(session.lastSeenAt)}</td>
                        <td style={{ textAlign: 'right', whiteSpace: 'nowrap' }}>
                          {session.isCurrent
                            ? <span className='text-muted' style={{ fontSize: '12px' }}>This session</span>
                            : <button
                                className={s.saveButton}
                                onClick={() => {
                                  revokeSession(session.sessionId).then(() => refreshSessions(setSessions));
                                }}
                              >Sign Out</button>
                          }
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        )}
      </div>
    </div>

    <div className='col-12 mt-4'>
      <Subtitle>Secure Sign Out</Subtitle>
      <div className={cardStyles.card + ' p-3'}>
        <div className='row'>
          <div className='col-10 col-lg-6'>
            <p>Sign out of all other sessions</p>
          </div>
          <div className='col-2 col-lg-6'>
            <button className={s.saveButton + ' float-right'} onClick={() => {
              logoutFromAllOtherSessions().then(() => refreshSessions(setSessions));
            }}>Sign Out</button>
          </div>
        </div>
      </div>
    </div>

    <div className='col-12 mt-4'>
      <Subtitle>Two Factor Authentication</Subtitle>
      <div className={cardStyles.card + ' p-3'}>
        <div className='row align-items-center'>
          <div className='col'>
            {twoFactorInfo === null
              ? <span className='text-muted'>Loading...</span>
              : <span>Status: <strong>{twoFactorInfo.enabled ? 'Enabled' : 'Disabled'}</strong></span>
            }
          </div>
          <div className='col-auto'>
            <button className={s.saveButton} onClick={() => store.setModal('TOTP_MANAGE')}>
              Manage
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
}

export default Security;
