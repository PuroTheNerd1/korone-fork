/**
 * Create group_previous_name table
 * @param {import('knex')} knex
 */
exports.up = async (knex) => {
    await knex.schema.createTable('group_previous_name', (t) => {
        t.bigIncrements('id').notNullable().unsigned();
        t.bigInteger('group_id').notNullable().unsigned();
        t.string('name', 255).notNullable();
        t.dateTime('created_at').notNullable().defaultTo(knex.fn.now());

        t.index(['group_id']);
    });
};

/**
 * Drop the group_previous_name table
 * @param {import('knex')} knex
 */
exports.down = async (knex) => {
    await knex.schema.dropTable('group_previous_name');
};
