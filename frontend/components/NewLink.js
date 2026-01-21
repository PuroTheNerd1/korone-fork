import NextLink from 'next/link';

/**
 * @param {string} className
 * @param {MouseEventHandler<HTMLAnchorElement>} onClick
 * @param {ReactNode|string|undefined|null} children
 * @param {string|null|undefined} href
 * @param {CSSProperties} style
 * @returns {JSX.Element}
 * @constructor
 */
const NewLink = ({className, onClick, children, href, style}) => {
    return <NextLink href={href || "#"} passHref>
        <a className={className} onClick={onClick} style={style}>
            {children}
        </a>
    </NextLink>
}

export default NewLink;