import axios from 'axios';
import conf from './config.js';
import { soap } from './soap.js';

/**
 *
 * @param {{RCC: number; XML: string; jobExpiration: number;}} param0
 */
const request = async ({ RCC, XML, jobExpiration }) => {
    try {
        let headers = {
            'Content-Type': 'text/xml',
        }
        let xml = soap(conf.baseUrl, jobExpiration, JSON.stringify(XML))
        const result = await axios.request({
            method: 'POST',
            url: `${conf.rccUrl}:${RCC}`,
            timeout: jobExpiration * 1000,
            data: xml,
            headers,
            //maxRedirects: 0,
        })
        return result;
    } catch (e) {
        throw new Error(e);
    }
}

export default request;