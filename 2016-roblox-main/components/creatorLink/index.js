import {itemNameToEncodedName} from "../../services/catalog";
import Link from "../link";
import { createUseStyles } from "react-jss";

const useStyles = createUseStyles({
  aLink: {
    textDecoration: 'none!important',
    color: '#00A2FF',
    '&:hover': {
      color: '#00A2FF',
      textDecoration: 'underline!important'
    }
  }
})

/**
 * Creator link
 * @param {{type: string | number; id: number; name: string;}} props 
 * @returns 
 */
const CreatorLink = (props) => {
  const url = (props.type === 'Group' || props.type === 1) ? '/My/Groups.aspx?gid=' + props.id : '/User.aspx?ID=' + props.id;
  const s = useStyles();
  return <Link href={url}>
    <a className={s.aLink}>
      {props.name}
    </a>
  </Link>
}

export default CreatorLink;