import { createUseStyles } from "react-jss"

const useStyles = createUseStyles({
  main: {
    minHeight: '95vh',
    paddingTop: '12px',
  },
  display: {
    display: 'flex',
    flexDirection: 'column',
    minHeight: '100vh',
  }
})

const MainWrapper = ({ mainFlex, children }) => {
  const s = useStyles();
  return <div className={`${s.main} ${mainFlex ? s.display : null}`}>
    {children}
  </div>
}

export default MainWrapper;