import React, { useEffect, useState } from "react";
import { createUseStyles } from "react-jss";
import GameCard from '../../newGameCard';
import ThumbnailStore from "../../../stores/thumbnailStore";
import { multiGetGameVotes } from "../../../services/games";

const useStyles = createUseStyles({
    gameCardsContainer: {
        whiteSpace: 'nowrap',
        listStyle: 'none',
        margin: 0,
        padding: 0,
        flexDirection: 'row',
        gap: '12px',
        height: '250px',
    },
});

/**
 * Recommendations
 * @param {{
* games: any[];
* }} props
* @returns
*/

const GameRow = props => {
    const s = useStyles();
    const store = ThumbnailStore.useContainer();
    const [games, setGames] = useState([]);

    var customWidth = null;

    useEffect(() => {
        if (props.games) {
            customWidth = 1 / props.games.length;
            //if (games.length !== 0) {
            const universeIds = props.games.map(game => game.universeId);
            const gamesNew = [];
            multiGetGameVotes({ universeIds }).then((votes) => {
                props.games.map((game) => {
                    const voteData = votes.find(vote => vote.id === game.universeId);
                    if (voteData) {
                        gamesNew.push({
                            ...game,
                            totalUpVotes: voteData.upVotes,
                            totalDownVotes: voteData.downVotes,
                        })
                    } else {
                        gamesNew.push(game);
                    }
                });
                setGames(gamesNew);
            });
            //}
        }
    }, [props])

    return <ul className={s.gameCardsContainer}>
        {
            games.map((game) => {
                var thumbnail;
                var gameThumbnail = store.getGameIcon(game.placeId, '420x420');
                gameThumbnail ? thumbnail = gameThumbnail : thumbnail = '/img/placeholder/icon_one.png';
                return <GameCard
                    name={game.name}
                    playerCount={game.playerCount || '?'}
                    likes={game.totalUpVotes || 0}
                    dislikes={game.totalDownVotes || 0}
                    creatorId={game.builderId}
                    creatorType={game.builderType}
                    creatorName={game.builder}
                    iconUrl={thumbnail}
                    year={game.year || 2012}
                    placeId={game.placeId}
                    width={customWidth}
                />
            })
        }
    </ul>
};

export default GameRow;