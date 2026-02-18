import { useEffect, useState } from "react";
import useCardStyles from "../../userProfile/styles/card";
import useFormStyles from "../styles/forms";
import Subtitle from "./subtitle";
import { logoutFromAllOtherSessions } from "../../../services/auth";
import { getTwoFactorInfo } from "../../../services/accountSettings";
import MyAccountStore from "../stores/myAccountStore";

const Security = props => {
  const cardStyles = useCardStyles();
  const s = useFormStyles();
  const store = MyAccountStore.useContainer();
  const [twoFactorInfo, setTwoFactorInfo] = useState(null);

  useEffect(() => {
    getTwoFactorInfo().then(setTwoFactorInfo);
  }, []);

  return <div className='row'>
    <div className='col-12 mt-2'>
      <Subtitle>Secure Sign Out</Subtitle>
      <div className={cardStyles.card + ' p-3'}>
        <div className='row'>
          <div className='col-10 col-lg-6'>
            <p>Sign out of all other sessions</p>
          </div>
          <div className='col-2 col-lg-6'>
            <button className={s.saveButton + ' float-right'} onClick={() => {
              logoutFromAllOtherSessions().then(() => {
                window.location.reload();
              })
            }}>Sign Out</button>
          </div>
        </div>
      </div>
    </div>

    <div className='col-12 mt-4'>
      <Subtitle>Two-Factor Authentication</Subtitle>
      <div className={cardStyles.card + ' p-3'}>
        {twoFactorInfo === null ? <p>Loading...</p> : <>
          <p className='mb-2'>
            Status: <strong>{twoFactorInfo.enabled ? 'Enabled' : 'Disabled'}</strong>
          </p>
          {!twoFactorInfo.enabled && <>
            <p className='text-muted mb-2' style={{ fontSize: '0.9em' }}>
              Scan the QR code below with Google Authenticator or any TOTP app, then enter the code to enable 2FA.
            </p>
            <img src={twoFactorInfo.qrCodeDataUrl} width={160} height={160} className='d-block mb-2' />
            <p className='text-muted mb-3' style={{ fontSize: '0.85em', wordBreak: 'break-all' }}>
              Manual key: {twoFactorInfo.secret}
            </p>
            <button className={s.saveButton} onClick={() => {
              store.setModal('TOTP_ENABLE');
            }}>Enable 2FA</button>
          </>}
          {twoFactorInfo.enabled && <>
            <button className={s.saveButton} style={{ background: 'var(--danger-color, #d9534f)' }} onClick={() => {
              store.setModal('TOTP_DISABLE');
            }}>Remove 2FA</button>
          </>}
        </>}
      </div>
    </div>
  </div>
}

export default Security;
