import {useRef, useState} from "react";
import { createContainer } from "unstated-next";

const FeedbackStore = createContainer(() => {
    const [feedbacks, setFeedbacks] = useState([]);
    const feedbacksRef = useRef(new Map());
    
    const addFeedback = (feedback, type) => {
        const id = Date.now();
        setFeedbacks(prev => [...prev, {feedback, type, visible: false, id}]);
        const feedbackVisible = setTimeout(() => {
            setFeedbacks(prev => prev.map(feedbackItem => (feedbackItem.id === id ? { ...feedbackItem, visible: true } : feedbackItem)))
        }, 100);
        const feedbackInvisible = setTimeout(() => {
            setFeedbacks(prev => prev.map(feedbackItem => (feedbackItem.id === id ? { ...feedbackItem, visible: false } : feedbackItem)))
        }, 4000);
        const feedbackDelete = setTimeout(() => {
            setFeedbacks(prev => prev.filter(e => e.id !== id));
            feedbacksRef.current.delete(id);
        }, 5000);
        feedbacksRef.current.set(id, { feedbackVisible, feedbackInvisible, feedbackDelete });
    };
    
    return {
        feedbacks,
        addFeedback,
    }
});

export default FeedbackStore;