import xp from 'ultimate-express'
import { RequestAvatarThumbnail, RequestAvatarHeadshot } from '../controllers/playerRCC.js'
import authMiddleware from '../middleware/auth.js'

const router = xp.Router();
router.post(`/thumbnail`, authMiddleware, RequestAvatarThumbnail);
router.post(`/headshot`, authMiddleware, RequestAvatarHeadshot);

export default router;