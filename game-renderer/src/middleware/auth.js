import response from '../util/response.js'
import conf from '../util/config.js'

const auth = async (req, res, next) => {
    const { authorization, userAgent } = req.headers
    if (authorization && authorization.startsWith('Bearer ')) {
        let token = authorization.slice(7)
        if (token && token === conf.authorization) {
            next()
        } else {
            return response(res, 'You are not authorized to use game-renderer!', 401, false)
        }
    } else {
        return response(res, 'You are not authorized to use game-renderer!', 401, false)
    }
}

export default auth;