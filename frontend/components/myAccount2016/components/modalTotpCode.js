import { useState } from "react";
import { enableTwoFactor, disableTwoFactor, getTwoFactorInfo } from "../../../services/accountSettings";
import ActionButton from "../../actionButton";
import MyAccountStore from "../stores/myAccountStore";
import useModalStyles from "../styles/modal";
import FeedbackStore from "../../../stores/feedback";
import { FeedbackType } from "../../../models/feedback";

const ModalTotpCode = ({ mode }) => {
  const store = MyAccountStore.useContainer();
  const feedback = FeedbackStore.useContainer();
  const [code, setCode] = useState('');
  const [error, setError] = useState(null);
  const [locked, setLocked] = useState(false);
  const s = useModalStyles();

  const submit = () => {
    if (!code || code.length !== 6) {
      setError('Please enter a 6-digit code');
      return;
    }
    setError(null);
    setLocked(true);
    const action = mode === 'enable' ? enableTwoFactor({ code }) : disableTwoFactor({ code });
    action.then(() => {
      store.setModal(null);
      feedback.addFeedback(
        mode === 'enable' ? '2FA has been enabled' : '2FA has been disabled',
        FeedbackType.SUCCESS
      );
    }).catch(e => {
      setError(e.response?.data?.errors?.[0]?.message || 'Invalid code');
    }).finally(() => {
      setLocked(false);
    });
  };

  return <div className='row ps-4'>
    <div className='col-12'>
      {error && <p className='text-danger mb-1'>{error}</p>}
      <input
        disabled={locked}
        type='text'
        className={s.input}
        placeholder='6-digit code'
        maxLength={6}
        value={code}
        onChange={e => setCode(e.currentTarget.value.replace(/\D/g, ''))}
        autoComplete='off'
      />
      <div className={s.confirmWrapper}>
        <ActionButton disabled={locked} label={mode === 'enable' ? 'Enable' : 'Remove'} onClick={submit} />
      </div>
    </div>
  </div>
}

export default ModalTotpCode;
