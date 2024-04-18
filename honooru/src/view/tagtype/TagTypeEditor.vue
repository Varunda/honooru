<template>
    <div>
        <app-menu></app-menu>

        <div v-if="types.state == 'idle'"></div>
        <div v-else-if="types.state == 'loading'">
            loading...
        </div>

        <div v-else-if="types.state == 'loaded'">

            <table class="table">
                <thead>
                    <tr class="table-secondary">
                        <th>ID</th>
                        <th>name</th>
                        <th>alias</th>
                        <th>color</th>
                        <th>dark text</th>
                        <th>order</th>
                        <td>delete</td>
                    </tr>
                </thead>

                <tbody>
                    <tr v-for="type in sortedTagTypes">
                        <td>
                            {{type.id}}
                        </td>

                        <td>
                            <input v-model="type.name" type="text" class="form-control" />
                        </td>

                        <td>
                            <div class="input-group">
                                <input v-model="type.alias" type="text" class="form-control" />
                            </div>
                        </td>

                        <td>
                            <div class="input-group">
                                <span class="input-group-text">
                                    #
                                </span>
                                <input type="text" v-model="type.hexColor" class="form-control" />
                                <input type="color" :value="'#' + type.hexColor" class="form-control form-control-color" readonly="readonly" disabled="disabled" />
                            </div>
                        </td>

                        <td>
                            dark text
                        </td>

                        <td>
                            <button class="btn btn-sm btn-primary" @click="orderDecrease(type.id)">
                                <span class="bi-chevron-up"></span>
                            </button>

                            <button class="btn btn-sm btn-primary" @click="orderIncrease(type.id)">
                                <span class="bi-chevron-down"></span>
                            </button>

                            <span class="border-right px-2"></span>

                            <button class="btn btn-sm btn-outline-info" @click="orderDecrease(type.id, true)">
                                <span class="bi-plus"></span>
                            </button>

                            <button class="btn btn-sm btn-outline-info" @click="orderIncrease(type.id, true)">
                                <span class="bi-dash"></span>
                            </button>

                            <span class="text-muted ml-3">
                                ({{type.order}})
                            </span>
                        </td>

                        <td>
                            delete
                        </td>
                    </tr>

                    <tr class="table-secondary">
                        <td colspan="7">

                        </td>
                    </tr>

                    <tr>
                        <td>
                            --
                        </td>

                        <td>
                            <input v-model="create.name" class="form-control" type="text" />
                        </td>

                        <td>
                            <input v-model="create.alias" class="form-control" type="text" />
                        </td>

                        <td>
                            <div class="input-group">
                                <span class="input-group-text">
                                    #
                                </span>
                                <input v-model="create.hexColor" class="form-control" type="text" />
                                <input type="color" :value="'#' + create.hexColor" class="form-control form-control-color" readonly="readonly" disabled="disabled" />
                            </div>
                        </td>

                        <td>
                            dark text
                        </td>

                        <td>
                            <button class="btn btn-sm btn-success" @click="insert">
                                create
                            </button>
                        </td>
                    </tr>

                </tbody>
            </table>

            <button class="btn btn-success" @click="save">
                save
            </button>
        </div>

    </div>
</template>

<script lang="ts">
    import Vue from "vue";
    import { Loadable, Loading } from "Loading";
    import Toaster from "Toaster";

    import { AppMenu } from "components/AppMenu";

    import { TagType, TagTypeApi } from "api/TagTypeApi";

    export const TagTypeEditor = Vue.extend({
        props: {

        },

        data: function() {
            return {
                types: Loadable.idle() as Loading<TagType[]>,
                copy: [] as TagType[],

                create: new TagType() as TagType
            }
        },

        created: function(): void {
            document.title = "Honooru / Tag Types";
        },

        mounted: function(): void {
            this.loadTypes();
        },

        methods: {
            loadTypes: async function(): Promise<void> {
                this.copy = [];
                this.types = Loadable.loading();
                this.types = await TagTypeApi.getAll();

                if (this.types.state == "loaded") {
                    this.copy = JSON.parse(JSON.stringify(this.types.data));
                }
            },

            save: async function(): Promise<void> {
                if (this.types.state != "loaded") {
                    console.warn(`cannot save: types.state is '${this.types.state}', not 'loaded'`);
                    return;
                }

                let reload: boolean = false;

                for (const type of this.types.data) {
                    const copyType: TagType | undefined = this.copy.find(iter => iter.id == type.id);
                    if (copyType != undefined
                        && copyType.name == type.name
                        && copyType.alias == type.alias
                        && copyType.hexColor == type.hexColor
                        && copyType.darkText == type.darkText
                        && copyType.order == type.order) {

                        console.log(`skipping update to ${type.id} as nothing has changed`);

                        continue;
                    }

                    console.log(`performing update to ${type.id}!`);

                    reload = true;
                    await TagTypeApi.update(type.id, type);
                }

                if (reload == true) {
                    await this.loadTypes();
                }
            },

            insert: async function(): Promise<void> {
                if (this.types.state != "loaded") {
                    return;
                }

                const maxOrder: number = Math.max(...this.types.data.map(iter => iter.order));
                console.log(`maxOrder is ${maxOrder + 1}`);
                this.create.order = maxOrder + 1;

                const l: Loading<TagType> = await TagTypeApi.insert(this.create);
                if (l.state == "loaded") {
                    Toaster.add("tag type added", `successfully insert new tag type ${l.data.name}`, "success");
                    await this.loadTypes();
                } else if (l.state == "error") {
                    Loadable.toastError(l, "failed to create tag type");
                } else {
                    console.error(`unchecked state of response (create): ${l.state}`);
                }
            },

            orderIncrease: async function(typeID: number, doSwap: boolean = false): Promise<void> {
                console.log(`increasing the order of ${typeID}`);
                if (this.types.state != "loaded") {
                    console.warn(`cannot increase order of ${typeID}: type.state is '${this.types.state}', not 'loaded'`);
                    return;
                }

                const type: TagType | undefined = this.types.data.find(iter => iter.id == typeID);
                if (type == undefined) {
                    console.warn(`cannot increase order of ${typeID}: no tag type with ID of ${typeID} found`);
                    return;
                }

                if (doSwap == true) {
                    console.log(`finding order of ${type.order}`);
                    const downType: TagType | undefined = this.types.data.find(iter => iter.order == type.order - 1);
                    if (downType != undefined) {
                        downType.order = type.order;
                        await TagTypeApi.update(downType.id, downType);
                    }
                }

                type.order += 1;
                await TagTypeApi.update(type.id, type);
                await this.loadTypes();
            },

            orderDecrease: async function(typeID: number, doSwap: boolean = false): Promise<void> {
                console.log(`decreasing the order of ${typeID}`);
                if (this.types.state != "loaded") {
                    console.warn(`cannot decrease order of ${typeID}: type.state is '${this.types.state}', not 'loaded'`);
                    return;
                }

                const type: TagType | undefined = this.types.data.find(iter => iter.id == typeID);
                if (type == undefined) {
                    console.warn(`cannot decrease order of ${typeID}: no tag type with ID of ${typeID} found`);
                    return;
                }

                if (doSwap == true) {
                    console.log(`finding order of ${type.order}`);
                    const downType: TagType | undefined = this.types.data.find(iter => iter.order == type.order - 1);
                    if (downType != undefined) {
                        downType.order = type.order;
                        await TagTypeApi.update(downType.id, downType);
                    }
                }

                type.order -= 1;
                await TagTypeApi.update(type.id, type);
                await this.loadTypes();
            }
        },

        computed: {

            sortedTagTypes: function(): TagType[] {
                if (this.types.state != "loaded") {
                    return [];
                }

                return [...this.types.data].sort((a, b) => {
                    return a.order - b.order;
                });
            }

        },

        components: {
            AppMenu
        }
    });
    export default TagTypeEditor;

</script>