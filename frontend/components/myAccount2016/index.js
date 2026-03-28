import React, { useEffect } from "react";
import { createUseStyles } from "react-jss";
import { useRouter } from "next/router";
import {getInventoryPrivacy, getTradePrivacy, getTradeValue, getPrivateMessagePrivacy} from "../../services/accountSettings";
import { getUserInfo } from "../../services/users";
import AuthenticationStore from "../../stores/authentication";
import AccountInfo from "./components/accountInfo";
import Customization from "./components/customization";
import ModalHandler from "./components/modalHandler";
import PrivacySettings from "./components/privacySettings";
import Tabs from "./components/tabs";
import MyAccountStore from "./stores/myAccountStore";
import Security from "./components/security";
import Social from "./components/social";

const hashToTab = {
  accountinfo: 'Account Info',
  security: 'Security',
  social: 'Social',
  privacy: 'Privacy',
  customization: 'Customization',
};

const tabToHash = {
  'Account Info': 'accountinfo',
  'Security': 'security',
  'Social': 'social',
  'Privacy': 'privacy',
  'Customization': 'customization',
};

const useStyles = createUseStyles({
  settingsRow: {
    background: 'var(--background-color)',
  },
})

const MyAccount = props => {
  const s = useStyles();

  const store = MyAccountStore.useContainer();
  const auth = AuthenticationStore.useContainer();
  const router = useRouter();

  useEffect(() => {
    const hash = router.asPath.split('#')[1];
    if (hash && hashToTab[hash.toLowerCase()]) {
      store.setTab(hashToTab[hash.toLowerCase()]);
    }
  }, [router.asPath]);

  useEffect(() => {
    if (typeof window !== 'undefined') {
      const hash = tabToHash[store.tab];
      if (hash) {
        history.replaceState(null, '', '#' + hash);
      }
    }
  }, [store.tab]);

  useEffect(() => {
    if (auth.isPending) return
    getUserInfo({
      userId: auth.userId
    }).then((d) => {
      store.setDescription(d.description);
    });

    getTradePrivacy().then(store.setTradePrivacy);
    getInventoryPrivacy().then(store.setInventoryPrivacy);
    getTradeValue().then(store.setTradeFilter);
    getPrivateMessagePrivacy().then(store.setPrivateMessagePrivacy);
    
  }, [auth.userId, auth.isPending]);
  if (auth.isPending || !auth.userId) return null;
  return <div className='container ssp'>
    <ModalHandler></ModalHandler>
    <div className={'row pb-2 ' + s.settingsRow}>
      <div className='col-12'>
        <h1 className='mt-0 mb-0'>My Settings</h1>
      </div>
      <div className='col-12'>
        <Tabs></Tabs>
      </div>
      <div className='col-12'>
        {store.tab === 'Account Info' ? <AccountInfo/> : null}
        {store.tab === 'Security' ? <Security /> : null}
        {store.tab === 'Social' ? <Social /> : null}
        {store.tab === 'Privacy' ? <PrivacySettings/> : null}
        {store.tab === 'Customization' ? <Customization/> : null}
      </div>
    </div>
  </div>
}

export default MyAccount;