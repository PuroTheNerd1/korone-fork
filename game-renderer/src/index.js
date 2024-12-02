import xp from "ultimate-express";
import imageRouter from './routers/imageRouter.js'
import conf from "./util/config.js";
import playerRouter from "./routers/playerRouter.js";
import gameRouter from "./routers/gameRouter.js";
import catalogRouter from "./routers/catalogRouter.js";

const app = xp();
app.use(xp.text({ limit: '250mb' }));
app.use(xp.json());
app.listen(conf.port, () => {
    console.log(`[info] game-renderer listening on port`, conf.port)
});

app.use('/image', imageRouter)
app.use('/player', playerRouter)
app.use('/game', gameRouter)
app.use('/catalog', catalogRouter)
app.get('/', async (req, res) => {
    res.status(500);
})