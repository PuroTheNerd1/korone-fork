import { createUseStyles } from "react-jss";

const useStyles = createUseStyles({
    modalBg: {
        background: 'rgba(0,0,0,0.8)',
        position: 'fixed',
        top: 0,
        width: '100%',
        height: '100%',
        left: 0,
        zIndex: 9999,
        display: 'flex',
        justifyContent: 'center',
        alignItems: 'center',
    },
    modalContainer:{
        height: '100%',
        width: '100%',
        outline: '0px',
        overflow: 'visible',
    },
    modalWrapper: {
        width: '400px',
        margin: '0 auto',
        marginTop: 'calc(50vh - 125px)',
    },
    modalDialog:{
        boxSizing: 'border-box',
        maxWidth: '100%',
        width: '400px',
        display: 'inline-block',
        textAlign: 'left',
        verticalAlign: 'middle',
        margin: 0
    },
    modalContent:{
        backgroundColor: '#fff',
        borderRadius: 0,
        position: 'relative',
        border: '1px solid rgba(0, 0, 0, 0.2)',
        backgroundClip: 'padding-box',
        outline: 0,
    },
    modalHeader:{
        borderColor: '#e3e3e3',
        textAlign: 'left',
        padding: '12px',
        borderBottom: '1px solid #e3e3e3',
        minHeight: '16.428571429px'
    },
    modalHeaderText:{
        fontSize: '16px',
        fontWeight: '500',
        lineHeight: '1.428571429',
        margin: 0,
        padding: '5px 0',
    },
    modalBody:{
        textAlign: 'left',
        padding: '12px',
        position: 'relative',
    },
    modalMessage:{
        fontWeight: '400',
        fontSize: '16px',
        lineHeight: '1.4em'
    },
    modalFooter:{
        borderTop: 0,
        margin: '0 12px 12px',
        padding: 0,
        textAlign: 'center',
        color: '#b8b8b8',
        fontSize: '10px',
        fontWeight: '500'
    },
    noDisplay:{
        display: 'none'
    }
});

const newModal = props => {
    const s = useStyles();

    const modalHeader = props.title;
    const modalTopBody = props.children;
    const footer = props.footerElements || props.footerText;

    const footerClass = footer ? '' : s.noDisplay;

    return <div className={s.modalBg}>
        <div className={s.modalContainer}>
            <div className={s.modalWrapper}>
                <div className={s.modalDialog}>
                    <div className={s.modalContent}>
                        <div className={s.modalHeader}>
                            <h5 className={s.modalHeaderText}>{modalHeader}</h5>
                        </div>
                        <div className={s.modalBody}>
                            {modalTopBody}
                        </div>
                        <div className={s.modalFooter + ' ' + footerClass}>
                            {footer}
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
}

export default newModal;