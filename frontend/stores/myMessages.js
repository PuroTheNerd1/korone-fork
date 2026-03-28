import { useEffect, useState } from "react";
import { createContainer } from "unstated-next";
import { getAnnouncements, getMessages } from "../services/privateMessages";

const READ_NOTIFICATIONS_KEY = 'readNotificationIds';

const MyMessagesStore = createContainer(() => {
  const limit = 10;
  const [notifications, setNotifications] = useState(null);
  const [messages, setMessages] = useState(null);
  const [offset, setOffset] = useState(0);
  const [total, setTotal] = useState(null);
  const [highlightedMessage, setHighlightedMessage] = useState(null);
  const [tab, setTab] = useState(null);
  const [checked, setChecked] = useState([]);
  const [readNotificationIds, setReadNotificationIds] = useState(() => {
    try {
      return JSON.parse(localStorage.getItem(READ_NOTIFICATIONS_KEY) || '[]');
    } catch {
      return [];
    }
  });

  const markNotificationAsRead = (id) => {
    if (readNotificationIds.includes(id)) return;
    const updated = [...readNotificationIds, id];
    setReadNotificationIds(updated);
    localStorage.setItem(READ_NOTIFICATIONS_KEY, JSON.stringify(updated));
  };

  useEffect(() => {
    getAnnouncements().then(setNotifications);
  }, []);

  const getMessagesForTab = (tab) => {
    setMessages(null);
    setTotal(null);
    getMessages({
      tab,
      offset,
      limit: limit
    }).then(res => {
      setMessages(res.collection);
      setTotal(res.totalPages * limit);
    });
  }

  useEffect(() => {
    console.debug('[info] use ms tab', tab);
    setChecked([]);
    setMessages(null);
    if (tab === 'notifications') {
      setMessages(notifications.collection);
      setTotal(notifications.collection.length);
    } else if (tab === 'inbox' || tab === 'sent' || tab === 'archive') {
      getMessagesForTab(tab);
    } else {
      setMessages(null);
    }
  }, [tab, offset]);

  return {
    limit,

    offset,
    setOffset,

    total,
    setTotal,

    messages,
    setMessages,

    notifications,
    setNotifications,

    readNotificationIds,
    markNotificationAsRead,

    highlightedMessage,
    setHighlightedMessage,

    checked,
    setChecked,

    tab,
    setTab,

    getMessagesForTab,
  }
});

export default MyMessagesStore;