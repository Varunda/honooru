<template>
    <div>
        <div class="row">
            <div class="col-6">
                <h4 class="wt-header">
                    Accounts
                </h4>

                <div v-if="accounts.state == 'loaded'" class="list-group">
                    <div v-for="account in accounts.data" class="list-group-item" @click="selectAccount(account.id)">
                        {{account.name}} / {{account.id}}
                    </div>
                </div>

                <div class="border-top pt-3 mt-3">
                    <div>
                        <div class="form-floating">
                            <input v-model="create.name" type="text" class="form-control" />
                            <label>name</label>
                        </div>

                        <div class="form-floating">
                            <input v-model.number="create.discordID" type="number" class="form-control" />
                            <label>discord ID</label>
                        </div>

                        <button class="btn btn-primary" type="button" @click="createAccount">create</button>
                    </div>
                </div>
            </div>

            <div class="col-6">
                <div class="wt-header">
                    groups for 
                    <span v-if="selected.account != null">
                        {{selected.account.name}} / {{selected.account.id}}
                    </span>
                    <span v-else class="text-muted">
                        no account selected
                    </span>
                </div>

                <div v-if="selected.groups.state == 'loaded'">
                    <div class="list-group">
                        <div v-for="group in groupsOfAccount" class="list-group-item" @click="selectGroup(group.groupID)">
                            {{group.name}}
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
</template>

<script lang="ts">
    import Vue from "vue";
    import { Loadable, Loading } from "Loading";

    import { AppAccount, AppAccountApi } from "api/AppAccountApi";
    import { AppAccountGroupMembership, AppAccountGroupMembershipApi } from "api/AppAccountGroupMembershipApi";
    import { AppGroup, AppGroupApi } from "api/AppGroupApi";

    import EventBus from "EventBus";

    interface NamedGroup {
        groupID: number;
        name: string;
    }

    export const AppAccounts = Vue.extend({
        props: {

        },

        data: function() {
            return {
                accounts: Loadable.idle() as Loading<AppAccount[]>,
                groups: Loadable.idle() as Loading<AppGroup[]>,

                selected: {
                    account: null as AppAccount | null,
                    groups: Loadable.idle() as Loading<AppAccountGroupMembership[]>
                },

                create: {
                    name: "" as string,
                    discordID: 0 as number
                }
            }
        },

        mounted: function() {
            this.loadAccounts();
            this.loadGroups();
        },

        methods: {
            loadAccounts: async function(): Promise<void> {
                this.accounts = Loadable.loading();
                this.accounts = await AppAccountApi.getAll();
            },

            loadGroups: async function(): Promise<void> {
                this.groups = Loadable.loading();
                this.groups = await AppGroupApi.getAll();
            },

            selectAccount: async function(accountID: number): Promise<void> {
                if (this.accounts.state != "loaded") {
                    console.warn(`cannot select account ${accountID}: this.accounts.state is ${this.accounts.state}, not "loaded"`);
                    return;
                }

                this.selected.account = this.accounts.data.find(iter => iter.id == accountID) ?? null;

                this.selected.groups = Loadable.loading();
                this.selected.groups = await AppAccountGroupMembershipApi.getByAccountID(accountID);
            },

            selectGroup: async function(groupID: number): Promise<void> {
                EventBus.$emit("select-group", groupID);
            },

            createAccount: async function(): Promise<void> {

            }
        },

        computed: {
            groupsOfAccount: function(): NamedGroup[] {
                if (this.selected.groups.state != "loaded" || this.groups.state != "loaded") {
                    return [];
                }

                const map: Map<number, AppGroup> = this.groupMap;
                return this.selected.groups.data.map((iter: AppAccountGroupMembership) => {
                    return {
                        groupID: iter.groupID,
                        name: map.get(iter.groupID)?.name ?? `<missing ${iter.groupID}>`
                    }
                });
            },

            groupMap: function(): Map<number, AppGroup> {
                if (this.groups.state != "loaded") {
                    return new Map();
                }

                const map: Map<number, AppGroup> = new Map();

                for (const group of this.groups.data) {
                    map.set(group.id, group);
                }

                return map;
            }
        },

        components: {

        }
    });
    export default AppAccounts;

</script>