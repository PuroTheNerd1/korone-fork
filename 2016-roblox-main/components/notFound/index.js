import {createUseStyles} from "react-jss";
import useButtonStyles from "../../styles/buttonStyles";
import ActionButton from "../actionButton";
import {useRouter} from "next/router";

const useStyles = createUseStyles({
    container: {
        '& img': {
            height: '300px',
            width: '300px',
            margin: '20px 0',
            display: 'block',
        }
    },
    buttonContainer: {
        padding: '3px 0',
        gap: '12px',
        display: 'flex',
        '& button': {
            padding: 9,
            width: '90px',
            lineHeight: '18px',
            fontSize: 18
        }
    },
    textContainer: {
        '& *': {
            marginBottom: 12,
            padding: '5px 0',
            lineHeight: '1em',
            textAlign: 'start',
        }
    },
});

const NotFound = () => {
    const s = useStyles();
    const router = useRouter();
    const buttonStyles = useButtonStyles();
    
    return <div className='col-12 h-100 flex justify-content-center align-items-center' style={{
        maxWidth: 970,
        width: 800,
        margin: '0 auto',
        marginTop: '10%',
    }}>
        <div className={`${s.container} section-content flex justify-content-between`}>
            <div className='flex justify-content-between flex-column w-50'>
                <div className={s.textContainer}>
                    <h3 style={{fontSize: '32px', fontWeight: 500}}>Page cannot be found or no longer exists</h3>
                    <h4 style={{fontSize: '16px', fontWeight: 400}}>404<span style={{padding: '0 5px'}}>|</span>Page
                        Not Found</h4>
                </div>
                <div className={s.buttonContainer}>
                    <ActionButton buttonStyle={buttonStyles.newBuyButton} label='Back' onClick={e => {
                        e.preventDefault();
                        router.back();
                    }}/>
                    <ActionButton buttonStyle={buttonStyles.newCancelButton} label='Home' onClick={e => {
                        e.preventDefault();
                        router.push('/home');
                    }}/>
                </div>
            </div>
            <img src='/img/404.png' alt='404'/>
        </div>
    </div>
}

export default NotFound;