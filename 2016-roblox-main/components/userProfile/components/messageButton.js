import React from "react";
import AuthenticationStore from "../../../stores/authentication";
import UserProfileStore from "../stores/UserProfileStore";
import Button from "./button";
import { createUseStyles } from "react-jss";

const useStyles = createUseStyles({
  
})

const MessageButton = props => {
  const store = UserProfileStore.useContainer();
  const auth = AuthenticationStore.useContainer();
  const s = useStyles()
  // TODO: check if we can message user, disable if user is not messageable
  return <Button style={{ width: 'auto' }} className={`text-dark`} onClick={() => {
    window.location.href='/messages/compose?recipientId=' + store.userId
  }}>
    {/*<a className={`text-dark`} >Message</a>*/}
    Message
  </Button>
}

export default MessageButton;