# switch_accounts_get_nickname
Gets the current name of the account ID Provided

## Arguments
| Argument | Type | Description |
| ---- | ---- | ---- |
| Account ID | Int | ID of the user account |

## Syntax
```
switch_accounts_get_nickname(id);
```

## Returns
The name of the account ID provided.

## Example
```gml
//print out account name
show_debug_message(switch_accounts_get_nickname(global.curuser));
```