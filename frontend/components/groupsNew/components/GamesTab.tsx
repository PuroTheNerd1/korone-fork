import Section from "./Section";

const GamesTab = ({}: {}) => {
    return <div>
        <Section header={"Games"} contentSectioned={true} className={"disabled"}>
            This group has not created any games yet.
        </Section>
    </div>
};

export default GamesTab;