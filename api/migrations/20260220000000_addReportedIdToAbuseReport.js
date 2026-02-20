/**
 * Add reported_asset_id and reported_user_id columns to abuse_report table
 * @param {import('knex')} knex
 */
exports.up = async (knex) => {
    await knex.schema.alterTable('abuse_report', (t) => {
        t.bigInteger('reported_asset_id').nullable().defaultTo(null);
        t.bigInteger('reported_user_id').nullable().defaultTo(null);
    });
};

exports.down = async (knex) => {
    await knex.schema.alterTable('abuse_report', (t) => {
        t.dropColumn('reported_asset_id');
        t.dropColumn('reported_user_id');
    });
};
