import { useState, useEffect } from "react";
import { enableTwoFactor, disableTwoFactor, getTwoFactorInfo } from "../../../services/accountSettings";
import ActionButton from "../../actionButton";
import MyAccountStore from "../stores/myAccountStore";
import useModalStyles from "../styles/modal";
import FeedbackStore from "../../../stores/feedback";
import { FeedbackType } from "../../../models/feedback";

const ModalManageTotp = () => {
  const store = MyAccountStore.useContainer();
  const feedback = FeedbackStore.useContainer();
  const [code, setCode] = useState('');
  const [error, setError] = useState(null);
  const [locked, setLocked] = useState(false);
  const [twoFactorInfo, setTwoFactorInfo] = useState(null);
  const s = useModalStyles();

  useEffect(() => {
    getTwoFactorInfo().then(setTwoFactorInfo);
  }, []);

  const submit = () => {
    if (!code || code.length !== 6) {
      setError('Please enter a 6-digit code');
      return;
    }
    setError(null);
    setLocked(true);
    const isEnabled = twoFactorInfo?.enabled;
    const action = isEnabled ? disableTwoFactor({ code }) : enableTwoFactor({ code });
    action.then(() => {
      store.setModal(null);
      feedback.addFeedback(
        isEnabled ? '2FA has been disabled' : '2FA has been enabled',
        FeedbackType.SUCCESS
      );
    }).catch(e => {
      setError(e.response?.data?.errors?.[0]?.message || 'Invalid code');
    }).finally(() => {
      setLocked(false);
    });
  };

  if (!twoFactorInfo) {
    return <div className='row ps-4'><div className='col-12'><p>Loading...</p></div></div>;
  }

  return <div className='row ps-4 pe-4'>
    <div className='col-12'>
      {!twoFactorInfo.enabled && <>
        <div style={{ textAlign: 'center', marginBottom: '8px' }}>
          <img src={twoFactorInfo.qrCodeDataUrl} width={160} height={160} />
        </div>
        <p style={{ fontSize: '0.85em', wordBreak: 'break-all', marginBottom: '8px' }}>
          <strong>Manual Key:</strong> {twoFactorInfo.secret}
        </p>
      </>}
      {error && <p className='text-danger mb-1'>{error}</p>}
      <input
        disabled={locked}
        type='text'
        className={s.input}
        placeholder='2FA Code'
        maxLength={6}
        value={code}
        onChange={e => setCode(e.currentTarget.value.replace(/\D/g, ''))}
        autoComplete='off'
      />
      <div className={s.confirmWrapper}>
        <ActionButton
          disabled={locked}
          label={twoFactorInfo.enabled ? 'Remove' : 'Enable'}
          onClick={submit}
        />
      </div>
      {!twoFactorInfo.enabled && <p style={{ fontSize: '0.85em', marginTop: '10px', marginBottom: 0 }}>
        <strong>Important:</strong> If you lose your 2FA code you won&apos;t be able to access your account again.
      </p>}
    </div>
  </div>;
}

export default ModalManageTotp;
