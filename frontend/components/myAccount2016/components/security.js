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
